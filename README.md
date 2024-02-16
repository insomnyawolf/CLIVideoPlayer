# CLIVideoPlayer

Simple CLI Video Player (without audio for now)

It started as an internal joke but also evolved quite a bit, i tried my best to optimize the code but i still didn't got it fast enough to play hq videos at a decent speed.

Update, i keep trying to optimize it once and again and actually it's probably fast enough to play videos on hd (oh well, it seems like what's limitng it is actually the windows terminal.....)

I don't think i can top up this amount of optimization again tho i even surprised myself this time(maybe i just did(?))

## 

## How to use

Compile and drag any video file into the executable

## To do

* Add a way to select the render resolution, texture filter, pixel proportions and such configs in the launching arguments or in a settings file.
* React to resolution changes
* Fix FPS Limiter
* Fix FPS Display
* Bypass windows console slow part (?)
  * FastPipe may help there i guess [SampleImplementation](https://github.com/cmuratori/termbench/blob/main/fast_pipe.h)
* Check why does FFMPEG doesn't wanna work on linux
* Full Cross-Platform Version

## Currently Broken

* Playback Speed

## Done

* Avoid String creation in loops
* Avoid Allocatin memory when possible
* More Intermediate Grey Tones (it has colors now)
* Multithread Decoding => Oh man, that was harder than it sounds

## Trivia

When i was on the first version it was slow, so slow that i thought i needed by force multi-tasked decoding so i started writing a multithreaded version.
I failed because i was inexperienced so i went back to the single threaded one and optimized it.
I managed to optimize it so much that i didn't really need multithreading anymore but i still wanted to learn it and used playing hd videos as a excuse.
After a while i managed to get it working (it was quite hard honestly, i was hitting deadlocks everywhere).
Because i'm obssesed i kept optimizing it as much as i could (this time by just being clever about how did i use memory).
This got me a x6 performance increase overall (nice).
Right now what i'm doing on the experimental branch (mostly refactor things and remove overheads when possible) i expect to get around a 30x performance improvement more
I don't have metrics to prove my following claims but i'm sucessfull i think i sucessfully managed to get a +50.000x times better performance compared with the first version.
Tldr, first version sucks, i know a lot of new things now and, and the thing became a nice playground to learn new things.

## Considering

* Removing any encoding that isn't ASCII
  * Since it's a single byte, maybe i can improve performance even more because i won't have to iterate in arrays for each color channel after i do that, just write the byte. 

## Contact

If you need anything, just open an issue or dm me anywhere =)
