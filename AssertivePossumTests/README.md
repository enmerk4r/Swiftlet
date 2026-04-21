![](../Assets/Misc/github-png.png)

# Assertive Possum Tests

This folder contains Grasshopper test definitions for Swiftlet built with [Assertive Possum](https://www.food4rhino.com/en/app/assertive-possum), a unit testing framework for Grasshopper files.

Assertive Possum brings a continuous integration style workflow to computational design:

- Plugin authors can use it to catch regressions when changing components.
- Grasshopper users can use it to verify that important definitions still behave correctly after upgrading plugins or Rhino environments.

## How These Tests Work

Each test file is a normal Grasshopper definition that uses Swiftlet components and wires results into Assertive Possum assertion components. A test runner then sends the definitions to Rhino.Compute, solves them, collects the assertion results, and produces a report.

## Where To Learn More

- Food4Rhino: <https://www.food4rhino.com/en/app/assertive-possum>
- GitHub: <https://github.com/enmerk4r/AssertivePossum>

Those links include setup instructions, example files, and documentation on how to create and run Assertive Possum test definitions.
