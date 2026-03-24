# Project Context: CPMCore Solution

## Solution structure
- CPMCore = ASP.NET Core MVC (UI layer: controllers + views)
- ServiceCore = business logic
- FacadeCore = orchestration / coordination layer
- DALCore = Entity Framework Core models & DbContext
- BOCore = shared business objects, enums, DTOs, helpers

## Architecture rules (VERY IMPORTANT)
- Never put business logic in CPMCore controllers
- Always use ServiceCore for business logic
- Use FacadeCore for orchestration between services when needed
- DALCore is strictly for data access (EF Core)
- BOCore contains shared models and should not depend on UI or EF

## EF Core guidelines
- Always use async/await
- Avoid unnecessary ToList()
- Use Includes only when needed
- Keep queries efficient and readable
- Do not mix tracking and non-tracking unintentionally

## Coding guidelines
- Follow existing patterns in the solution
- Do not introduce new architecture patterns
- Prefer extending existing code over rewriting
- Keep methods small and readable
- Use clear naming conventions consistent with the project

## UI guidelines
- Use Bootstrap with Porto Admin theme
- Keep UI consistent with existing pages
- Reuse existing components and layouts

## Behavior
- Always analyze existing code before adding new code
- Follow the established structure of the solution
- Do not duplicate logic that already exists

## Database change strategy (VERY IMPORTANT)

- NEVER modify DALCore models for database changes
- NEVER suggest EF Core migrations
- ALWAYS generate SQL scripts for database changes
- SQL Server syntax must be used

- Database is leading, not code-first
- All schema changes must be done via SQL scripts

- After generating SQL, optionally suggest how DALCore should be updated manually
