Feature: Donor organization read access
	As a member of a donor organization or an administrator
	I want to see the donations and contributions of the organization
	So that the organization can be reported on transparently

Scenario: Member views the donations of their organization
	Given The user has an access token
	And There is a donor organization on the time for code platform
	And The user is affiliated with the donor organization
	And The donor organization has an active donation of 10 hours
	When The user requests the donations of the donor organization
	Then 1 donations are returned

Scenario: Member views the contributions of their organization
	Given The user has an access token
	And There is a published project on the time for code platform
	And There is a donor organization on the time for code platform
	And The user has already logged 4 hours on the project
	And The user is affiliated with the donor organization
	When The user requests the contributions of the donor organization
	Then The contributions of the donor organization add up to 4 hours

Scenario: Contributions of colleagues are included
	Given The user has an access token
	And There is a published project on the time for code platform
	And There is a donor organization on the time for code platform
	And The user is affiliated with the donor organization
	And Another user is registered as a contributor affiliated with the donor organization
	And Another user has already logged 3 hours on the project
	When The user requests the contributions of the donor organization
	Then The contributions of the donor organization add up to 3 hours

Scenario: Contributions of people outside the organization are left out
	Given The user has an access token
	And There is a published project on the time for code platform
	And There is a donor organization on the time for code platform
	And The user is affiliated with the donor organization
	And Another user has already logged 3 hours on the project
	When The user requests the contributions of the donor organization
	Then The contributions of the donor organization add up to 0 hours

Scenario: Deleted contributions are not listed
	Given The user has an access token
	And There is a published project on the time for code platform
	And There is a donor organization on the time for code platform
	And The user has already logged 4 hours on the project
	And The user is affiliated with the donor organization
	And The user has already deleted the contribution
	When The user requests the contributions of the donor organization
	Then The contributions of the donor organization add up to 0 hours

Scenario: Administrator views the donations of any organization
	Given The user has an administrator access token
	And There is a donor organization on the time for code platform
	And The donor organization has an active donation of 10 hours
	When The user requests the donations of the donor organization
	Then 1 donations are returned

Scenario: Administrator views the contributions of any organization
	Given The user has an administrator access token
	And There is a published project on the time for code platform
	And There is a donor organization on the time for code platform
	And Another user is registered as a contributor affiliated with the donor organization
	And Another user has already logged 3 hours on the project
	When The user requests the contributions of the donor organization
	Then The contributions of the donor organization add up to 3 hours

Scenario: Donations are returned page by page
	Given The user has an access token
	And There is a donor organization on the time for code platform
	And The user is affiliated with the donor organization
	And The donor organization has an active donation of 10 hours
	And The donor organization has an active donation of 5 hours
	When The user requests page 2 of the donations of the donor organization with 1 donations per page
	Then 1 donations are returned
	And The donor organization has 2 donations in total

Scenario: Contributions are returned page by page
	Given The user has an access token
	And There is a published project on the time for code platform
	And There is a donor organization on the time for code platform
	And The user is affiliated with the donor organization
	And The user has already logged 4 hours on the project
	And Another user is registered as a contributor affiliated with the donor organization
	And Another user has already logged 3 hours on the project
	When The user requests page 2 of the contributions of the donor organization with 1 contributions per page
	Then 1 contributions are returned
	And The donor organization has 2 contributions in total

Scenario: Organization without donations has an empty list
	Given The user has an access token
	And There is a donor organization on the time for code platform
	And The user is affiliated with the donor organization
	When The user requests the donations of the donor organization
	Then An empty list of donations is returned

Scenario: User from another organization cannot view the donations
	Given The user has an access token
	And There is a donor organization on the time for code platform
	When The user requests the donations of the donor organization
	Then The request is rejected as forbidden

Scenario: User from another organization cannot view the contributions
	Given The user has an access token
	And There is a donor organization on the time for code platform
	When The user requests the contributions of the donor organization
	Then The request is rejected as forbidden

Scenario: Donations require an account
	Given The user does not have an access token
	And There is a donor organization on the time for code platform
	When The user requests the donations of the donor organization
	Then The request is rejected as unauthorized

Scenario: Contributions require an account
	Given The user does not have an access token
	And There is a donor organization on the time for code platform
	When The user requests the contributions of the donor organization
	Then The request is rejected as unauthorized

Scenario: Donations of an unknown organization are not found
	Given The user has an access token
	And There is no donor organization with the given identifier
	When The user requests the donations of the donor organization
	Then The request is rejected as not found

Scenario: Contributions of an unknown organization are not found
	Given The user has an access token
	And There is no donor organization with the given identifier
	When The user requests the contributions of the donor organization
	Then The request is rejected as not found
