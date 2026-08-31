Feature: Update a donor organization
	As a user
	I want to update the details of a donor organization on the time for code platform
	So that the organization's information remains current

Scenario: User updates an existing donor organization
	Given There is a donor organization on the time for code platform
	When The user updates the donor organization with a new name
	Then The donor organization is updated on the time for code platform

Scenario: Updating a non-existent donor organization returns not found
	Given There is no donor organization with the given identifier
	When The user updates the donor organization with a new name
	Then The user is informed the donor organization was not found

Scenario: Updating a donor organization to a name already in use is rejected
	Given There is a donor organization on the time for code platform
	And There is already a donor organization with the same name on the time for code platform
	When The user updates the donor organization with a name already in use
	Then The user is informed the donor organization name is already in use
