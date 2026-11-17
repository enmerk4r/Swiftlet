# Layout standard for the Grasshopper 2 example files

Every `.ghz` file in this folder follows the rules below. They exist so that a new user can read an
example without tracing wires through text, and so that all examples look like they were made by one hand.

`build/examples/gh2_layout.py check` measures the rules marked **(checked)** and must report zero problems.
The others need eyes: render the file and look at it before you call it done.

## Text

Grasshopper 2 canvas text has no size setting. It has a font, bold or italic, a colour and an alignment.
The hierarchy is therefore made with weight and case.

| Level | Used for | Style |
|---|---|---|
| Column heading | A column of related groups, for example `SIMPLE REQUESTS` | Sans, **bold, all capitals**, one line |
| Group title | One group, for example `GET Request` | Sans, **bold**, one line |
| Note | An explanation of one component or one row | Sans, *italic*, wrapped at 260 |
| Flag | A call to action on one component, for example `ENABLE ME!` | Sans, **bold**, red, one line |

- All text is left-aligned and unrotated. **(checked)**
- Titles and headings are free text above the group. Do not use the built-in group header as well. **(checked)**
- Write notes as plain sentences. Say what the reader should do or notice, not how the file was built.

## Groups, titles and columns

- Every component, parameter and panel is in a group. **(checked)**
- Everything that is drawn inside a group is a member of that group. **(checked for notes)**
- A group title is flush with the left edge of its group and its bottom edge is 8 units above the group. Every
  group has exactly one. **(checked)**
- A column heading is flush with the same left edge and its bottom edge is 56 units above the first group of
  the column. **(checked)**
- Groups in a column share one left edge. The first group of every column starts on the same line.
- Stacked groups are 56 units apart; groups are never closer than 16 units and never overlap. **(checked)**
- A file with a single flow needs no column heading: one group with a title is enough.

## Inside a group

- Data flows left to right. No wire runs right to left. **(checked)**
- One row per idea. Inputs on the left edge of the group, the result panel at the right end of the row.
- A panel that only inspects an intermediate value sits above the row, clear of the wire that continues past it.
- Nothing overlaps, and objects are at least 6 units apart. **(checked)**
- No wire passes behind a component, a parameter, a panel or text. **(checked, approximately)**
- Inputs that feed the same component are stacked in one column with equal gaps, in the order of the pins
  they feed. A toggle or value snapped onto an input still gets its own place in that column.
- Value boxes are wide enough to show their whole text.
- Rows are evenly spaced. Leave room for a note above or below a row instead of squeezing it in.

## Notes

- A note belongs to one object. It sits directly above or below that object, left edges aligned, 6 to 18
  units away. **(checked)**
- A note is inside the group of the object it describes, and is a member of that group. **(checked)**
- A note never touches a wire, a component or another note. **(checked)**
- Prefer the note above the first input of the row it explains. Put it below only when a wire has to pass
  above.

## Flags

- A request component that would send something the moment the file opens is saved disabled, with a flag
  reading `ENABLE ME!` directly under it: left edges aligned, 8 units below, member of the same group, bold
  and red (`"role": "flag", "colour": "Red"` in `apply`). **(checked: a flag must sit under a disabled
  component)**
- Make room for the flag if it would touch a wire, a name label or another object.
- Flags are for the reader's next action only. Explanations are notes.

## Behaviour that must not change

Layout work changes positions, text and group membership only. `gh2_layout.py apply` refuses to save if a
component, parameter or wire went missing, but it cannot tell whether a value changed: do not edit values,
and re-run the example's own check afterwards if it has one.

Request components that are saved disabled stay disabled. The `disable` op is the one exception to the rule
above: use it only for a component that gets a flag (see Flags).

## Working on a file

```
python tests/Swiftlet.Cases/slot.py --stop-file C:\path\my.stop          # start a Rhino 9, note the port
python build/examples/gh2_layout.py check  --port PORT Examples/x/file.ghz
python build/examples/gh2_layout.py dump   --port PORT Examples/x/file.ghz --json dump.json
python build/examples/gh2_members.py dump.json [GROUPID]                 # objects row by row, with their sources
python build/examples/gh2_layout.py apply  --port PORT Examples/x/file.ghz ops.json
python build/examples/gh2_layout.py render --port PORT Examples/x/file.ghz out.png [--region X,Y,W,H]
```

`apply` takes a list of operations: move an object to a left/top position, put a note above or below an
object, put a title or heading over a group, add or restyle text, widen value boxes, add an object to a
group. The full list is at the top of `build/examples/gh2_layout.py`.

Two helpers write operations for the common cases:

- `build/examples/gh2_columns.py` stacks groups into aligned columns and gives each a title.
- `build/examples/gh2_push_right.py` makes room after value boxes that had to be widened.

Things worth knowing:

- Objects snapped onto a component move with it. `apply` replays every placement once at the end, so give
  absolute positions and do not try to compensate.
- A group's outline is its members plus a 14 unit margin. It does not refresh while you move members from a
  script; `apply` works it out from the members.
- A component or parameter with a name of its own shows that name centred underneath. The label is not
  part of the object's bounds; the tools estimate its size from the text and treat it as part of the object.
  A long name can stick out to the left of the component, so look at the render.
- GH2 solves a file the moment it is shown on the canvas. The tools switch solving off while they run, so no
  example sends a request or opens a port. If you open a file any other way, it will run.
- Lay out the inside of each group first, then run the column step, then check, then render and look.
