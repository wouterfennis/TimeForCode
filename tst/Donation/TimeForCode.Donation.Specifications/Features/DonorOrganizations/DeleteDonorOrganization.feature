Feature: Delete a donor organization
	As a user
	I want to delete a donor organization from the time for code platform
	So that organizations that no longer participate are removed

Scenario: User deletes an existing donor organization
	Given There is a donor organization on the time for code platform
	When The user deletes the donor organization
	Then The donor organization is deleted from the time for code platform

Scenario: Deleting a non-existent donor organization returns not found
	Given There is no donor organization with the given identifier
	When The user deletes the donor organization
	Then The user is informed the donor organization was not found
