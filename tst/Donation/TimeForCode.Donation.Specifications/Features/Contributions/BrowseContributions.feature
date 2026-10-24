Feature: Browse time contributions
	As a user
	I want to see who has contributed time to a project and how much
	So that I can judge the activity and community behind the project

Scenario: User views the contributions of a project
	Given There is a published project with logged contributions
	When The user requests the contributions of the project
	Then The contributions of the project are returned
	And Each contribution includes the hours, the contributor and the time it was logged

Scenario: Viewing contributions does not require authentication
	Given There is a published project with logged contributions
	When The user requests the contributions of the project without an account
	Then The contributions of the project are returned

Scenario: Hours logged at different moments add up
	Given There is a published project on the time for code platform
	And The user has already logged 3 hours on the project
	And The user has already logged 2 hours on the project
	When The user requests the contributions of the project
	Then 2 contributions are returned
	And The contributions of the project add up to 5 hours

Scenario: Contributions are listed from earliest to latest
	Given There is a published project with contributions logged at different moments
	When The user requests the contributions of the project
	Then The contributions are ordered from earliest to latest

Scenario: Contribution with a GitHub reference links to GitHub
	Given There is a published project with a contribution that has a GitHub reference
	When The user requests the contributions of the project
	Then The contribution includes a link to the GitHub reference

Scenario: Contributions of other projects are not included
	Given There is a published project with logged contributions
	And Another project has logged contributions
	When The user requests the contributions of the project
	Then Only the contributions of the project are returned

Scenario: Project without contributions returns an empty list
	Given There is a published project on the time for code platform
	When The user requests the contributions of the project
	Then An empty list of contributions is returned

Scenario: User requests the contributions of a project that does not exist
	Given There is no project with the given identifier
	When The user requests the contributions of the project
	Then The request is rejected as not found

Scenario: Requesting contributions with a malformed project identifier is rejected
	When The user requests the contributions of the project with the identifier "not-an-identifier"
	Then The request is rejected as invalid
