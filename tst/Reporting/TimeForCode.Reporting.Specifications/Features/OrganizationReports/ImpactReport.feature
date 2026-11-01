Feature: Organization impact report
	As a member of a donor organization
	I want to see how many hours my organization pledged and logged and which projects it supported
	So that I can show transparently what my organization delivered

Scenario: Member views the impact report of their organization
	Given The user has an access token
	And The user is a member of the donor organization
	And The donor organization has an active donation of 10 hours
	And The user has logged 4 hours that count towards the donation
	When The user requests the impact report of the donor organization
	Then The impact report shows 10 hours pledged and 4 hours logged
	And The impact report shows 1 project supported

Scenario: Impact report breaks the totals down per project
	Given The user has an access token
	And The user is a member of the donor organization
	And The donor organization has donations to 2 different projects
	When The user requests the impact report of the donor organization
	Then The impact report shows 2 projects supported
	And Each project in the impact report shows its hours pledged and hours logged

Scenario: Impact report of an organization without donations is empty
	Given The user has an access token
	And The user is a member of the donor organization
	And The donor organization has no donations
	When The user requests the impact report of the donor organization
	Then The impact report shows 0 hours pledged and 0 hours logged
	And The impact report shows 0 projects supported

@live
Scenario: Non-member cannot view the impact report
	Given The user has an access token
	And There is a donor organization on the time for code platform
	When The user requests the impact report of the donor organization
	Then The request is rejected as forbidden

@live
Scenario: Impact report requires an account
	Given The user does not have an access token
	And There is a donor organization on the time for code platform
	When The user requests the impact report of the donor organization
	Then The request is rejected as unauthorized

@live
Scenario: Impact report of an unknown organization is not found
	Given The user has an access token
	And There is no donor organization with the given identifier
	When The user requests the impact report of the donor organization
	Then The request is rejected as not found

@live
@liveonly
Scenario: Administrator views the impact report of a registered organization on the live platform
	Given The user has an administrator access token
	And There is a donor organization on the time for code platform
	When The user requests the impact report of the donor organization
	Then The impact report shows 0 hours pledged and 0 hours logged
	And The impact report shows 0 projects supported
