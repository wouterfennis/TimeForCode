Feature: Browse donations and their progress
	As a donor or a project visitor
	I want to see donations with their progress
	So that I can follow how many pledged hours have been delivered

Scenario: Donor sees their own donations with progress
	Given The user has an access token
	And The user has an active donation of 10 hours with 4 hours logged
	When The user requests their donations
	Then The donations of the user are returned
	And The donation shows 10 hours committed and 4 hours logged
	And The donation shows a progress of 40 percent

Scenario: Donor without donations sees an empty list
	Given The user has an access token
	When The user requests their donations
	Then An empty list of donations is returned

Scenario: Donor does not see donations of other users
	Given The user has an access token
	And There is an active donation of another user
	When The user requests their donations
	Then An empty list of donations is returned

@live
Scenario: Requesting donations without being logged in is rejected
	Given The user does not have an access token
	When The user requests their donations
	Then The request is rejected as unauthorized

Scenario: Visitor sees the donations received by a project
	Given There is a published project with 2 donations
	When The user requests the donations of the project
	Then 2 donations are returned
	And Each donation shows its status, hours committed, hours logged and progress

Scenario: Project without donations returns an empty list
	Given There is a published project on the time for code platform
	When The user requests the donations of the project
	Then An empty list of donations is returned

Scenario: Donations of an unknown project are rejected
	Given There is no project with the given identifier
	When The user requests the donations of the project
	Then The request is rejected as not found
