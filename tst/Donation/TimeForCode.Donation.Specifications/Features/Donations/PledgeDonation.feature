Feature: Pledge hours to a project
	As a donor organization or an individual
	I want to pledge developer hours to an active project
	So that the project receives help and my commitment is tracked

Scenario: User pledges hours as an individual
	Given The user has an access token
	And There is a published project on the time for code platform
	When The user pledges 20 hours to the project as an individual
	Then The donation is recorded on the time for code platform
	And The donation is pending
	And The donation shows 20 hours committed and 0 hours logged

Scenario: User pledges hours for a donor organization
	Given The user has an access token
	And There is a published project on the time for code platform
	And There is a donor organization on the time for code platform
	When The user pledges 20 hours to the project for the donor organization
	Then The donation is recorded on the time for code platform
	And The donation is pending

Scenario: Pledge for both an organization and an individual is rejected
	Given The user has an access token
	And There is a published project on the time for code platform
	And There is a donor organization on the time for code platform
	When The user pledges 20 hours to the project as an individual and for the donor organization
	Then The user is informed the donor is invalid

Scenario: Pledge without a donor is rejected
	Given The user has an access token
	And There is a published project on the time for code platform
	When The user pledges 20 hours to the project without a donor
	Then The user is informed the donor is invalid

Scenario: Pledge for an unknown donor organization is rejected
	Given The user has an access token
	And There is a published project on the time for code platform
	And There is no donor organization with the given identifier
	When The user pledges 20 hours to the project for the donor organization
	Then The user is informed the donor organization was not found

Scenario Outline: Pledge with invalid hours is rejected
	Given The user has an access token
	And There is a published project on the time for code platform
	When The user pledges <hours> hours to the project as an individual
	Then The user is informed the hours are invalid

	Examples:
		| hours |
		| 0     |
		| -5    |

Scenario: Only active projects can receive pledges
	Given The user has an access token
	And There is a project that is in draft
	When The user pledges 20 hours to the project as an individual
	Then The user is informed the project cannot receive donations

Scenario: Pledge for a project that does not exist is rejected
	Given The user has an access token
	And There is no project with the given identifier
	When The user pledges 20 hours to the project as an individual
	Then The user is informed the project was not found

Scenario: Pledge without being logged in is rejected
	Given The user does not have an access token
	And There is a published project on the time for code platform
	When The user pledges 20 hours to the project as an individual
	Then The user is informed they must be logged in
