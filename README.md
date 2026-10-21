<p align="center"><img src="docs/logo.svg" width="96" alt="" /></p>

# modeplan

modeplan is an offline permission-analysis CLI for people who write `chmod` commands, installation scripts, Dockerfiles, and archive tools. It explains changes to the 12 permission bits (the rwx bits for owner, group, and others, plus setuid, setgid, and sticky) and analyzes text listings of paths and modes. It does not change file permissions or inspect files on disk; it reads only the listing files you name, or standard input when you use `-`.

[![CI](https://github.com/Gulliby-development/modeplan/actions/workflows/ci.yml/badge.svg)](https://github.com/Gulliby-development/modeplan/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Why

A `chmod` expression is easy to write and hard to predict: what does `a=rX` do to a directory, what does an empty WHO do with the umask, which letters clear the setuid bit, and what would the same expression do across a whole tree? `modeplan` answers these from arguments and a text listing, with exact rules and no file system access, so the output is the same on every machine and fits in a bug report or a test fixture.

## Install

Install the .NET 10 SDK, clone the repository, then build the console project from the repository root:

```text
git clone https://github.com/Gulliby-development/modeplan.git
cd modeplan
```

Build with:

```text
dotnet build
```

The build produces `modeplan.dll`. Run it with the .NET host, for example:

```text
dotnet src/Modeplan/bin/Debug/net10.0/modeplan.dll --help
```

To run directly from source, use `dotnet run --project src/Modeplan/Modeplan.csproj -- --help`.

## Usage

The help output lists all commands and their arguments:

```text
usage: modeplan apply MODE EXPR [--dir] [--umask OCT]
       modeplan plan OLD NEW
       modeplan match LISTING TEST... [--kind f|d]
       modeplan audit LISTING [--strict]
       modeplan tree LISTING EXPR [--umask OCT]
```

The listing examples below use a file named `tree.txt`. Create it by saving this text as UTF-8; it is example input and is not shipped with the program:

```text
# a small tree
d 0755 src/
f 0644 src/main.c
f 0755 build.sh
f 4755 bin/su
d 1777 /tmp
f 0666 notes.txt
d 0777 shared/
f 0600 id_rsa
f 0044 odd
d 0644 odd dir/
```

For example, `modeplan apply 0644 u+x,go-r` shows each step of the change:

```text
STEP  ACTION  MODE  SYMBOLIC
0     start   0644  rw-r--r--
1     u+x     0744  rwxr--r--
2     go-r    0700  rwx------
result      0700 rwx------
changed     3 bits
```

## Commands

- `modeplan apply MODE EXPR [--dir] [--umask OCT]` applies a numeric or symbolic permission expression to one mode. Example: `modeplan apply 0644 u+x,go-r`.
- `modeplan plan OLD NEW` prints a canonical symbolic expression that changes one mode into another. Example: `modeplan plan 0644 4755`.
- `modeplan match LISTING TEST... [--kind f|d]` selects listing entries using exact, all-bits, or any-bits permission tests. Example: `modeplan match tree.txt -111`.
- `modeplan audit LISTING [--strict]` reports risky permission combinations. Example: `modeplan audit tree.txt`.
- `modeplan tree LISTING EXPR [--umask OCT]` previews the changes an expression would make to each listing entry. Example: `modeplan tree tree.txt a+X,g-w`.

The audit example above prints:

```text
"notes.txt" MA001 world-writable file
"shared/" MA001 world-writable directory without the sticky bit
"odd" MA005 owner lacks r that group or other has
"odd dir/" MA006 directory readable but not searchable for owner
4 findings
```

### Options

| Option | Commands | Description |
|---|---|---|
| `--dir` | `apply` | Treat MODE as a directory (matters for `X`). |
| `--umask OCT` | `apply`, `tree` | Umask of 1 to 3 octal digits, default `022`. |
| `--kind f\|d` | `match` | Look only at files (`f`) or directories (`d`). |
| `--strict` | `audit` | Also report group-writable entries (`MA007`). |

## Exit codes

- `0`: the command completed with its positive result. For `apply`, `plan`, and `tree`, this means a mode changed; for `match`, at least one entry matched; for `audit`, no findings were reported.
- `1`: the command completed with its negative result. For `apply` and `tree`, nothing changed; for `plan`, the modes were equal; for `match`, no entries matched; for `audit`, one or more findings were reported.
- `2`: an argument, expression, or input error occurred. The diagnostic is written to standard error.

## Development

From the repository root, build, verify formatting, and run the tests:

```text
dotnet build
dotnet format --verify-no-changes
dotnet test
```

The program uses only the .NET base class library. The tests exercise parsing and commands with in-memory inputs, with a single process integration test for malformed UTF-8 on standard input.

## License

MIT, see [LICENSE](LICENSE).

---

Built with BAKED
