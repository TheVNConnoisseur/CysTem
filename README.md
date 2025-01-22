# CysTem
Tool that allows for the unpacking and packing of sound files offered in C,System powered visual novels (created by [Cyberworks](http://cyberworks.jp)).

### Notes on its usage
1. The .dat files containing the audio files can be reconstructed by using [CSystemTools](https://github.com/satan53x/CSystemTools).
2. The tool has only been tested (and will NOT be tested with other games) with [Noroware Kyoushitsu \~Kounai Itsudemo Dokodemo Yaranakucha?!\~](https://vndb.org/v39838). Still, pull requests will be accepted and tested to see if they work correctly.

### How are j0 (voice files), k0 (sound effects) and u0 (background music) files structured?
While the code also documents how these formats are structured, here it is also the same information on a more accessible manner:
All these formats are simply bog-standard OGG Vorbis files, with the *OggS* signature swapped with either *Tink* or *Song*. Besides that, they only have the following **3611 bytes XOR swapped** with a key that will differ between which signature they are using. 

Any byte that might come after the one located in offset 3615 is left intact.

Besides that, the only other thing worth mentioning is that for some reason, u0 files have another header of 12 bytes before the signature, but those seem to be random data that serves no purpose at all.
