# Clean Code Architecture Guidelines for .NET Core Projects

## Architecture Principles
- Apply Clean Architecture with clear separation of concerns.
- Use a layered structure: Presentation → Application → Domain → Infrastructure.
- Ensure dependencies flow inward; outer layers depend on inner layers only.
- Employ dependency injection for loose coupling.
- Keep the domain layer independent of frameworks and external concerns.

## Project Structure
- Organize by feature when appropriate, not just by technical layer.
- Use meaningful namespaces reflecting domain and architecture.
- Separate interfaces from implementations.
- Group related files in logical folders.

## Vertical Slicing
- Organize code by feature or module (vertical slice) instead of by technical layer.
- Each slice should be self-contained and include all layers (Presentation, Application, Domain, Infrastructure) for a specific feature.
- Create a dedicated folder for each vertical slice/module, potentially within a top-level `src/` folder.
- If the existing code already not in `src` folder, let it be. 
- This approach enhances modularity, reduces coupling between features, and improves maintainability.

## Coding Standards
- Follow C# conventions and .NET naming guidelines.
- Write self-documenting code with clear, descriptive names.
- Keep methods small and focused on a single responsibility.
- Use XML documentation for public APIs and complex logic.
- Limit method parameters to three or fewer when possible.

## Domain Layer
- Model entities with rich behavior, not just data.
- Use value objects for concepts without identity.
- Implement domain services for cross-entity operations.
- Define repository interfaces in the domain layer.

## Application Layer
- Implement use cases as application services or command/query handlers.
- Use DTOs for data transfer across boundaries.
- Apply CQRS when beneficial for separating reads and writes.
- Validate input at the application boundary.

## Infrastructure Layer
- Implement repository interfaces from the domain layer.
- Handle external concerns: database, file system, external services.
- Use the repository pattern for data access.
- Apply the unit of work pattern for transaction management.

## Presentation Layer
- Keep controllers thin and focused on HTTP concerns.
- Use view models tailored to UI needs.
- Implement robust error handling and proper status codes.
- Follow RESTful principles for API design.

## Testing
- Write unit tests for domain and application logic.
- Use integration tests for infrastructure components.
- Implement end-to-end tests for critical paths.
- Use test doubles (mocks, stubs) appropriately.

## Error Handling
- Use exceptions for exceptional cases only, not control flow.
- Create custom exception types for domain-specific errors.
- Implement global exception handling.
- Log exceptions with relevant context.

## Performance
- Use async/await for I/O-bound operations.
- Implement caching where appropriate.
- Use pagination for large data sets.
- Profile and optimize critical paths.

## Security
- Validate all user input.
- Implement authentication and authorization.
- Protect against common web vulnerabilities (XSS, CSRF, etc.).
- Use HTTPS for all communications.
- Never expose sensitive information in logs or error messages.