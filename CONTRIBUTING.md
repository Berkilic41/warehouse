# Contributing

Thank you for your interest in contributing to the Warehouse & Inventory Management System!

## Getting Started

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes
4. Add tests for any new business logic
5. Run the test suite: `dotnet test`
6. Commit your changes: `git commit -m "feat: add your feature"`
7. Push to your branch: `git push origin feature/your-feature-name`
8. Open a Pull Request

## Code Style

- Follow the 3-layer architecture: `Warehouse.Data` → `Warehouse.Service` → `Warehouse.Web`
- All SQL queries must be parameterized — no string concatenation
- Complex reporting queries belong in stored procedures (`Database/002_StoredProcedures.sql`)
- Stock movements must remain atomic (single transaction for `MovementItems` + `Products.CurrentStock`)

## Pull Request Guidelines

- Keep PRs focused on a single concern
- Include a clear description of what changed and why
- Ensure all existing tests pass
- Add tests for new features or bug fixes (see `tests/Warehouse.Tests/`)

## Reporting Issues

Please use [GitHub Issues](../../issues) to report bugs or request features.
