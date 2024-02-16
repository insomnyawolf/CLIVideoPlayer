// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;

namespace CLIVideoPlayer;

// Stripped down to just what we need
[DebuggerDisplay("Current Count = {m_currentCount}")]
public class CustomSemaphoreSlim : IDisposable
{
    internal static bool IsSingleProcessor => Environment.ProcessorCount == 1;
    //internal static readonly int SpinCountforSpinBeforeWait = IsSingleProcessor ? 1 : 35;
    internal static readonly int SpinCountforSpinBeforeWait = IsSingleProcessor ? 1 : 12;

    // The semaphore count, initialized in the constructor to the initial value, every release call increments it
    // and every wait call decrements it as long as its value is positive otherwise the wait will block.
    // Its value must be between the maximum semaphore value and zero
    private volatile int m_currentCount;

    // The maximum semaphore value, it is initialized to Int.MaxValue if the client didn't specify it. it is used
    // to check if the count exceeded the maximum value or not.
    private readonly int m_maxCount;

    // The number of synchronously waiting threads, it is set to zero in the constructor and increments before blocking the
    // threading and decrements it back after that. It is used as flag for the release call to know if there are
    // waiting threads in the monitor or not.
    private int m_waitCount;

    /// <summary>
    /// This is used to help prevent waking more waiters than necessary. It's not perfect and sometimes more waiters than
    /// necessary may still be woken, see <see cref="WaitUntilCountOrTimeout"/>.
    /// </summary>
    private int m_countOfWaitersPulsedToWake;

    // Object used to synchronize access to state on the instance.  The contained
    // Boolean value indicates whether the instance has been disposed.
    private readonly StrongBox<bool> m_lockObjAndDisposed;

    // Act as the semaphore wait handle, it's lazily initialized if needed, the first WaitHandle call initialize it
    // and wait an release sets and resets it respectively as long as it is not null
    private volatile ManualResetEvent? m_waitHandle;

    // No maximum constant
    private const int NO_MAXIMUM = int.MaxValue;

    public int CurrentCount => m_currentCount;

    public WaitHandle AvailableWaitHandle
    {
        get
        {
            CheckDispose();

            if (m_waitHandle is null)
            {
                lock (m_lockObjAndDisposed)
                {
                    m_waitHandle ??= new ManualResetEvent(m_currentCount != 0);
                }
            }

            return m_waitHandle;
        }
    }

    public CustomSemaphoreSlim(int initialCount)
        : this(initialCount, NO_MAXIMUM)
    {
    }

    public CustomSemaphoreSlim(int initialCount, int maxCount)
    {
        if (initialCount < 0 || initialCount > maxCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialCount), initialCount, "SR.SemaphoreSlim_ctor_InitialCountWrong");
        }

        // validate input
        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, "SR.SemaphoreSlim_ctor_MaxCountWrong");
        }

        m_maxCount = maxCount;
        m_currentCount = initialCount;
        m_lockObjAndDisposed = new StrongBox<bool>();
    }

    [UnsupportedOSPlatform("browser")]
    public bool Wait(int millisecondsTimeout = Timeout.Infinite, CancellationToken cancellationToken = default)
    {
        CheckDispose();

        if (millisecondsTimeout < -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(millisecondsTimeout), millisecondsTimeout, "SR.SemaphoreSlim_Wait_TimeoutWrong");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Perf: Check the stack timeout parameter before checking the volatile count
        if (millisecondsTimeout == 0 && m_currentCount == 0)
        {
            // Pessimistic fail fast, check volatile count outside lock (only when timeout is zero!)
            return false;
        }

        uint startTime = 0;
        if (millisecondsTimeout != Timeout.Infinite && millisecondsTimeout > 0)
        {
            startTime = TimeoutHelper.GetTime();
        }

        bool waitSuccessful = false;
        bool lockTaken = false;

        try
        {
            // Perf: first spin wait for the count to be positive.
            // This additional amount of spinwaiting in addition
            // to Monitor.Enter()'s spinwaiting has shown measurable perf gains in test scenarios.
            if (m_currentCount == 0)
            {
                // Monitor.Enter followed by Monitor.Wait is much more expensive than waiting on an event as it involves another
                // spin, contention, etc. The usual number of spin iterations that would otherwise be used here is increased to
                // lessen that extra expense of doing a proper wait.
                int spinCount = SpinCountforSpinBeforeWait * 4;

                SpinWait spinner = default;
                while (spinner.Count < spinCount)
                {
                    spinner.SpinOnce(sleep1Threshold: -1);

                    if (m_currentCount != 0)
                    {
                        break;
                    }
                }
            }
            Monitor.Enter(m_lockObjAndDisposed, ref lockTaken);
            m_waitCount++;


            // If the count > 0 we are good to move on.
            // If not, then wait if we were given allowed some wait duration

            OperationCanceledException? oce = null;

            if (m_currentCount == 0)
            {
                if (millisecondsTimeout == 0)
                {
                    return false;
                }

                // Prepare for the main wait...
                // wait until the count become greater than zero or the timeout is expired
                try
                {
                    waitSuccessful = WaitUntilCountOrTimeout(millisecondsTimeout, startTime, cancellationToken);
                }
                catch (OperationCanceledException e) { oce = e; }
            }

            // Now try to acquire.  We prioritize acquisition over cancellation/timeout so that we don't
            // lose any counts when there are asynchronous waiters in the mix.  Asynchronous waiters
            // defer to synchronous waiters in priority, which means that if it's possible an asynchronous
            // waiter didn't get released because a synchronous waiter was present, we need to ensure
            // that synchronous waiter succeeds so that they have a chance to release.
            Debug.Assert(!waitSuccessful || m_currentCount > 0,
                "If the wait was successful, there should be count available.");
            if (m_currentCount > 0)
            {
                waitSuccessful = true;
                m_currentCount--;
            }
            else if (oce is not null)
            {
                throw oce;
            }

            // Exposing wait handle which is lazily initialized if needed
            if (m_waitHandle is not null && m_currentCount == 0)
            {
                m_waitHandle.Reset();
            }
        }
        finally
        {
            // Release the lock
            if (lockTaken)
            {
                m_waitCount--;
                Monitor.Exit(m_lockObjAndDisposed);
            }
        }

        // If we had to fall back to asynchronous waiting, block on it
        // here now that we've released the lock, and return its
        // result when available.  Otherwise, this was a synchronous
        // wait, and whether we successfully acquired the semaphore is
        // stored in waitSuccessful.

        return waitSuccessful;
    }

    [UnsupportedOSPlatform("browser")]
    private bool WaitUntilCountOrTimeout(int millisecondsTimeout, uint startTime, CancellationToken cancellationToken)
    {
        int remainingWaitMilliseconds = Timeout.Infinite;

        // Wait on the monitor as long as the count is zero
        while (m_currentCount == 0)
        {
            // If cancelled, we throw. Trying to wait could lead to deadlock.
            cancellationToken.ThrowIfCancellationRequested();

            if (millisecondsTimeout != Timeout.Infinite)
            {
                remainingWaitMilliseconds = TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout);
                if (remainingWaitMilliseconds <= 0)
                {
                    // The thread has expires its timeout
                    return false;
                }
            }
            // ** the actual wait **
            bool waitSuccessful = Monitor.Wait(m_lockObjAndDisposed, remainingWaitMilliseconds);

            // This waiter has woken up and this needs to be reflected in the count of waiters pulsed to wake. Since we
            // don't have thread-specific pulse state, there is not enough information to tell whether this thread woke up
            // because it was pulsed. For instance, this thread may have timed out and may have been waiting to reacquire
            // the lock before returning from Monitor.Wait, in which case we don't know whether this thread got pulsed. So
            // in any woken case, decrement the count if possible. As such, timeouts could cause more waiters to wake than
            // necessary.
            if (m_countOfWaitersPulsedToWake != 0)
            {
                --m_countOfWaitersPulsedToWake;
            }

            if (!waitSuccessful)
            {
                return false;
            }
        }

        return true;
    }

    public int MustAvailable(int number)
    {
        var diff = number - m_currentCount;
        var num = Release(diff);
        return num;
    }

    public int Release(int releaseCount = 1)
    {
        CheckDispose();

        if (releaseCount < 1)
        {
            return 0;
        }

        int returnCount;

        lock (m_lockObjAndDisposed)
        {
            // Read the m_currentCount into a local variable to avoid unnecessary volatile accesses inside the lock.
            int currentCount = m_currentCount;
            returnCount = currentCount;

            // If the release count would result exceeding the maximum count, throw SemaphoreFullException.
            if (m_maxCount - currentCount < releaseCount)
            {
                throw new SemaphoreFullException();
            }

            // Increment the count by the actual release count
            currentCount += releaseCount;

            // Signal to any synchronous waiters, taking into account how many waiters have previously been pulsed to wake
            // but have not yet woken
            int waitCount = m_waitCount;
            Debug.Assert(m_countOfWaitersPulsedToWake <= waitCount);
            int waitersToNotify = Math.Min(currentCount, waitCount) - m_countOfWaitersPulsedToWake;
            if (waitersToNotify > 0)
            {
                // Ideally, limiting to a maximum of releaseCount would not be necessary and could be an assert instead, but
                // since WaitUntilCountOrTimeout() does not have enough information to tell whether a woken thread was
                // pulsed, it's possible for m_countOfWaitersPulsedToWake to be less than the number of threads that have
                // actually been pulsed to wake.
                if (waitersToNotify > releaseCount)
                {
                    waitersToNotify = releaseCount;
                }

                m_countOfWaitersPulsedToWake += waitersToNotify;
                for (int i = 0; i < waitersToNotify; i++)
                {
                    Monitor.Pulse(m_lockObjAndDisposed);
                }
            }

            m_currentCount = currentCount;

            // Exposing wait handle if it is not null
            if (m_waitHandle is not null && returnCount == 0 && currentCount > 0)
            {
                m_waitHandle.Set();
            }
        }

        // And return the count
        return returnCount;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            WaitHandle? wh = m_waitHandle;
            if (wh is not null)
            {
                wh.Dispose();
                m_waitHandle = null;
            }

            m_lockObjAndDisposed.Value = true;
        }
    }

    private void CheckDispose()
    {
        ObjectDisposedException.ThrowIf(m_lockObjAndDisposed.Value, this);
    }
}

public static class TimeoutHelper
{
    public static uint GetTime()
    {
        return (uint)DateTime.Now.Millisecond;
    }

    public static int UpdateTimeOut(uint startTime, int milisecondsTimeout)
    {
        var original = GetTime();
        var timeDiff = startTime - original;
        var remeaning = (uint)milisecondsTimeout - timeDiff;
        return (int)remeaning;
    }
}