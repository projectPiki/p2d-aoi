# pikmin2-aoi

An open-source all-in-one command-line tool for cleaning up the Pikmin 2 decompilation repository before committing changes. This tool automates file formatting, de-duplication, and generates a report of recommended files to decompile.

## Features

- **File Formatting**: Automatically formats `.cpp`, `.h`, and `.c` files in the `src/` and `include/` directories using `clang-format` to maintain code consistency.
- **Decompilation TODO List**: Scans through the repository to identify unlinked or redundant files and generates a `docs/recommended_todo.md` report, listing files to decompile and linking them by size and location.
- **Redundant File Cleanup**: Identifies and deletes unnecessary assembly (`.s`) files that have already been linked to prevent duplication.

### Command Output

- **`docs/recommended_todo.md`**: This file lists files to decompile, organized by folder and sorted by size for convenience.
- **Console Output**:
  - Lists formatted files.
  - Displays redundant `.s` files that were deleted.

## License

This project is licensed under the Unlicense. See the [LICENSE](LICENSE) file for details.