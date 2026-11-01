# MacroHardCraftLauncher reconstruction

This repository contains a partial static reconstruction and build-validation evidence.

Uploaded now:
- reconstructed metadata/reports;
- 60 C# source files from the recovered source tree;
- CRC and PE-model verification data.

The local staging repository also contains the 17.2 MB hybrid DLL, the 15.7 MB VM DLL, and the 233 MB complete VM IL source. Those large artifacts were not pushed here because the GitHub connector cannot transfer Git LFS objects and the shell network path is unavailable.

The VM extraction currently contains 57 types, 5591 methods, 5541 method bodies, and 6795360 IL instructions. The original sample was never executed. Runtime equivalence is not claimed while VM token/initializer mapping and one assembly reference remain unresolved.