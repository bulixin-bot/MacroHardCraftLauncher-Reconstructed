# MacroHardCraftLauncher reconstruction

This repository is a static reconstruction/build-validation package.

- `MacroHardCraftLauncher.hybrid.dll` contains the reconstructed main assembly plus the real VM IL tree.
- `VmSource.from-il.dll` is the real 57-type VM source library.
- `VmSource.il` is the complete IL source and is tracked with Git LFS in the local staging repository.
- `BUILD-REPORT.txt` records exact method/body/instruction counts and remaining limitations.

The original sample was never executed. Runtime equivalence is not claimed until the remaining VM token/initializer and one assembly-reference mapping are resolved.
