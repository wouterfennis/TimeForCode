Feature: Browse donor organizations
	As a user
	I want to browse donor organizations registered on the time for code platform
	So that I can see which organizations are supporting open source contributors

Scenario: User browses the donor organizations listing
	Given There are donor organizations on the time for code platform
	When The user requests the list of donor organizations
	Then A paginated list of donor organizations is returned

Scenario: Browsing donor organizations does not require authentication
	Given There are donor organizations on the time for code platform
	When The user requests the list of donor organizations without an account
	Then A paginated list of donor organizations is returned

Scenario: User views a specific donor organization
	Given There is a donor organization on the time for code platform
	When The user requests the donor organization details
	Then The donor organization details are returned

Scenario: Requesting a non-existent donor organization returns not found
	Given There is no donor organization with the given identifier
	When The user requests the donor organization details
	Then The user is informed the donor organization was not found
