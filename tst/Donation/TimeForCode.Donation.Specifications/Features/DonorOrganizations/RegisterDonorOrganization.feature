Feature: Register a donor organization
	As a user
	I want to register a donor organization on the time for code platform
	So that the organization's donated developer time can be tracked

Scenario: User registers a new donor organization
	When The user registers a donor organization with a valid name
	Then The donor organization is registered on the time for code platform

Scenario: Registering a donor organization with a duplicate name is rejected
	Given There is already a donor organization with the same name on the time for code platform
	When The user registers a donor organization with a valid name
	Then The user is informed the donor organization name is already in use

Scenario: Registering a donor organization without a name is rejected
	When The user registers a donor organization without a name
	Then The user is informed the donor organization cannot be created
