Feature: Contribution history export
	As a member or administrator of a donor organization
	I want to export the full contribution history of my organization
	So that I can use it for internal reporting

Scenario: Member exports the contribution history as CSV
	Given The user has an access token
	And The user is a member of the donor organization
	And The donor organization has an active donation of 10 hours
	And The user has logged 4 hours that count towards the donation
	When The user exports the contribution history of the donor organization as CSV
	Then The export contains the documented columns
	And The export lists a contribution of 4 hours

Scenario: Member exports the contribution history as JSON
	Given The user has an access token
	And The user is a member of the donor organization
	And The donor organization has an active donation of 10 hours
	And The user has logged 4 hours that count towards the donation
	When The user exports the contribution history of the donor organization as JSON
	Then The export contains the documented fields
	And The export lists a contribution of 4 hours

@live
Scenario: Administrator exports the contribution history of any organization
	Given The user has an administrator access token
	And There is a donor organization on the time for code platform
	When The user exports the contribution history of the donor organization as CSV
	Then The export contains the documented columns

Scenario: Export of an organization without contributions is empty
	Given The user has an access token
	And The user is a member of the donor organization
	And The donor organization has no donations
	When The user exports the contribution history of the donor organization as JSON
	Then The export lists no contributions

@live
Scenario: Non-member cannot export the contribution history
	Given The user has an access token
	And There is a donor organization on the time for code platform
	When The user exports the contribution history of the donor organization as CSV
	Then The request is rejected as forbidden

@live
Scenario: Export requires an account
	Given The user does not have an access token
	And There is a donor organization on the time for code platform
	When The user exports the contribution history of the donor organization as JSON
	Then The request is rejected as unauthorized

@live
Scenario: Export of an unknown organization is not found
	Given The user has an access token
	And There is no donor organization with the given identifier
	When The user exports the contribution history of the donor organization as CSV
	Then The request is rejected as not found
