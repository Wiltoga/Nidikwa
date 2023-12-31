## Nidikwa

Nidikwa is the predecessor to [Idikwa](https://github.com/Wiltoga/Idikwa), a software I made to record the last seconds/minutes of audio on a computer. The former had a few issues, was slow and could use a lot of memory for its cache.

### New features

* Nidikwa will be a Windows service now ! That means, its core functions (recording, putting in queue, ...) won't require any UI, making it way lighter than Idikwa.
* The recording cache will use the filesystem as a cache, to enable bigger durations without increasing the RAM usage, which will be way lower than ever.
* Queued item will also be stored on the filesystem using an unique extension. That means it will use even less RAM, and at the same time it keeps the queue between the service restarts.
* The audio edition will be separate from the service to have the best performances possible on the service.
* Nidikwa will be shipped with a SDK dll for people who want to interract with it, and it will be able to do (mostly) what the UI will do, including the audio edition.
* Nidikwa will also be shipped with a console .exe to interract with the service in CLI. It won't be as complete as the SDK as it will only use the core functions of the service, so no audio edition. You will still be able to export a queued audio to a structured folder holding the data.
* Finally, Nidikwa will have a brand new UI (using WPF, maybe), it will have more features on the editing side, like zooming in the timeline, removing parts in the middle of an audio (not sure yet on this one), or other stuff that will come to my mind later.

### Old features

Just as a reminder, Idikwa (the old one) is a software to record the last seconds/minutes of multiple audio endpoints (your microphone, speaker, ...) and putting it in a queue for later (for the duration of the .exe). After that, you can select a starting and ending point of the audio to export it to a mp3 file.
