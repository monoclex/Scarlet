# EEUniverse.Library

This code was taken from the `better-buffering` fork of EEUniverse.Library, which adds a better way to buffer to the library. https://github.com/SirJosh3917/Library/tree/better-buffering
That fork originates from EEUniverse/Library on github, located https://github.com/EEUniverse/Library

The primary reason for maintaining a custom copy of the library is to integrate performant code, and invoking C methods to do tasks that may take a significant amount of time - such as deserializing a world.

In addition, code can be easier benchmarked and modified nicer.