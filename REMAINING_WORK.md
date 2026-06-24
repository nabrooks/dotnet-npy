# Remaining Work / Roadmap

This file tracks work that was **deliberately deferred** during the comprehensive
review-and-fix effort. The correctness bugs, the dispose/leak/hang/crash cliffs,
the numpy header interop, the modern API surface (`Span<T>`, `NpyFile.Open<T>`),
and the IBM/`BinaryPrimitives` dedup are all **done and on `main`**. What follows
is the backlog: optimizations, larger refactors, and known gaps — none of which
are required for the library to function correctly today.

Rough priority order within each section. "Risk" is the chance of regressing
working, tested behavior; "Value" is the practical payoff.

---

## Performance / throughput

### 1. `ArrayPool<byte>` for the reader/writer scratch buffers — Value: med, Risk: med
`BigEndianBinaryReader`/`Writer` and `LittleEndianBinaryReader`/`Writer` each
allocate a **16 MB** scratch buffer (`new byte[BaseTwoPower.TwentyFour]`) on the
Large Object Heap. A `NpyFileBuffered` holds one reader + one writer, so ~34 MB of
LOH per open file. This is a *per-open* cost (not per read/write), so it doesn't
hit operation throughput, but it is real GC pressure for many-file workloads.

- Rent from `ArrayPool<byte>.Shared` in the ctor, return in `Dispose`.
- **Blocker / why deferred:** a few methods reassign the field
  (`buffer = new byte[count * 4]` in `LittleEndianBinaryReader.ReadIbmSingles`,
  etc.). Pooling requires those to return-the-old + rent-bigger, or the design must
  guarantee the buffer is never reassigned. Resolve that first.
- Also worth reconsidering the **size**: `NpyFileBuffered` only ever requests
  2 MB chunks (`maxBufferSize = BaseTwoPower.TwentyOne`), so most of the 16 MB is
  never touched. The 8× headroom may be unnecessary.

### 2. Eliminate boxing in scalar `Read<T>(T)` / `Write<T>(T value)` — Value: low, Risk: low
These dispatch via `(T)(object)value`, which boxes on every scalar call. The bulk
paths already use the dedicated typed array overloads, so this only costs on scalar
generic dispatch — not the throughput-critical path. Replace with
`Unsafe.As`/`Unsafe.BitCast` if it ever shows up in a profile.

### 3. `RandomAccess`-based buffered I/O — Value: med, Risk: med-high
`NpyFileBuffered` shares a single `Stream` between the reader and the writer, which
is the root of the disposal-ownership awkwardness. .NET 6+ `RandomAccess` offers
offset-based file reads/writes without a shared `Stream` object, which would
untangle ownership and remove a seek/`Stream` layer.

### 4. BenchmarkDotNet harness — Value: med, Risk: none
There is currently **no** way to validate perf claims. Add a small benchmark project
comparing: indexer read vs `AsSpan()` vs the old `T[] this[Range]` copy; buffered vs
memmap sequential/random access; per-element vs bulk array paths; across a few file
sizes. This should exist before attempting items 1 or 3.

---

## Refactors / code health

### 5. Merge the Big/Little `BinaryReader`/`BinaryWriter` families — Value: med, Risk: high
~800 lines of near-identical code remain between the big- and little-endian
reader/writer pairs. A shared generic core parameterized by endianness (using
`BinaryPrimitives.{Read,Write}XxxBigEndian`/`...LittleEndian`) would collapse most
of it.

- **Caution:** the LE non-IBM array methods currently use `Buffer.BlockCopy` (a
  single bulk `memcpy`) which is the *fastest* path on a little-endian host. Any
  merge must preserve that fast path for the common case and only fall back to a
  per-element loop for the (rare) cross-endian case — otherwise it's a throughput
  regression. This is why a blanket "use BinaryPrimitives everywhere" is the wrong
  move.

### 6. Delete or quarantine the IBM/360 float cluster — Value: med, Risk: low-med
`IbmConverter`, `IbmSingle`, `IbmBigEndianBitConverter`, `IbmLittleEndianBitConverter`,
the `ReadIbm*`/`WriteIbm` methods, and `IbmFloat` are **off-purpose for a numpy
library** — `.npy` has no IBM hex-float dtype (this looks carried over from a
SEG-Y / seismic codebase). They're now consolidated and hang-safe, but still dead
weight. Options: delete them (and the `SerializationTests` that exercise them), or
move them to a clearly-separate/optional component.
- Related: `IbmSingle.ToIeeeSingle()` still throws `NotImplementedException`, and
  the 64-bit `LittleEndianBinaryReader.ReadIbmDouble` is marked untested and uses
  32-bit masks on 64-bit data (almost certainly wrong) — only the hang was guarded.

### 7. Move `ShortGuid` to the test project — Value: low, Risk: low
`ShortGuid` is public API in the shipping library but is only used to generate
unique temp filenames **in tests**. It bloats the public surface of an IO library
for no production purpose.

### 8. Scalar reads still use manual byte-shuffles — Value: low, Risk: low
`ReadInt16/ReadInt32/ReadSingle/...` in the BE reader still hand-roll the swap.
They're correct and fast; converting to `BinaryPrimitives` for consistency with the
array methods is purely cosmetic.

### 9. Build-warning cleanup — Value: low, Risk: low
~95 warnings remain (nullable-reference, `CA2013` `ReferenceEquals` boxing in
`ShortGuid`, unboxing warnings, unused locals in the memmap header parser). None
affect behavior; a focused pass would clean them up.

---

## Behavior / design gaps

### 10. Buffered: no dirty-flag → reads write back — Value: med, Risk: med
`NpyFileBuffered` shares one `_buffer` for reads and writes with **no dirty flag**,
and the slice getters call `FlushBuffer()` before reading. So pure-read operations
re-write the buffer to disk (wasteful, and a hazard if the buffer is ever stale).
Add an `_isDirty` flag and only flush on actual writes.

### 11. Thread-safety contract — Value: med, Risk: low (docs) / high (impl)
`NpyFileBuffered`, `BigArray`, the readers/writers, and `Disposable` are all built
on mutable shared state and are **not thread-safe**. `Disposable` was hardened
against concurrent double-dispose (`Interlocked`), but nothing else is. At minimum,
**document** single-threaded-owner semantics; properly supporting concurrency is a
larger design change.

### 12. Big-endian via memmap is silently wrong — Value: med, Risk: low
`NpyFileMemmap` reads/writes raw native-endian memory and **ignores the file's byte
order**, so a big-endian `.npy` opened via memmap returns byte-swapped garbage
(only `NpyFileBuffered` honors endianness). The memmap type should either throw for
non-native-endian files or document the limitation prominently. (The `AsSpan` XML
docs already note the native-endian constraint.)

### 13. numpy v2.0 header support — Value: low, Risk: low
`BuildHeaderBytes` throws if the header dict exceeds 64 KB (`ushort` length). Very
high-dimensional shapes would need the v2.0 format (4-byte header length, magic
version `2.0`). Rare, but unsupported.

---

## Testing gaps

### 14. Real numpy-generated fixture — Value: med, Risk: none
The repo references `Data/float_2d_counter__301_5000.npy` for
`ReadSequential2DFloat`, but the fixture was **never committed**, so that test
can't run. Commit a small real numpy-written `.npy` (ideally a few dtypes /
fortran+C order), and add a round-trip test that shells out to numpy when available
to confirm cross-tool interop.

### 15. Multi-block `BigArray.Resize` is untestable — Value: low, Risk: low
The grow/shrink-within-a-block and resize-to-zero paths are covered, but the
**multi-block** path can't be exercised without ~2 GB allocations (block size is a
fixed `int.MaxValue / sizeof(T)` and not injectable). Make `_blockSize` injectable
(internal ctor) so the multi-block branch — where the original `Resize` bug lived —
can be unit-tested cheaply.

### 16. Concurrency / dispose-race tests — Value: low, Risk: none
The `Interlocked` dispose guard is unverified by test (hard to assert
deterministically). A stress test could give some confidence.

---

## Project / tooling

### 17. Target framework — Value: med, Risk: low
The projects target **`net7.0`, which is out of support**. Bump to a current LTS
(`net8.0`/`net10.0`). The local SDK here is 9.x and tests only run via
`DOTNET_ROLL_FORWARD=LatestMajor`; a supported TFM removes that friction.

### 18. `.gitignore` / line-ending noise — Value: low, Risk: none
`.gitignore`, `LICENSE`, and `README.md` carry large CRLF/normalization diffs in the
working tree (a Windows-mount artifact). Normalize once (`.gitattributes` with
`* text=auto`) to stop them showing up as spurious changes.

---

_Last updated as part of the review-and-fix effort. Items above are backlog, not
blockers — the library builds clean (0 errors) and the test suite is green._
