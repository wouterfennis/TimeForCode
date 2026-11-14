Feature: Public impact statistics
	As a visitor of the time for code platform
	I want to see the total hours contributed and the organizations supported
	So that I can see the impact of the platform without an account

Scenario: Visitor sees the totals without an account
	Given The platform has 2 donor organizations with 10 logged hours each
	And The user does not have an access token
	When The user requests the public statistics
	Then The public statistics show 20 hours and 2 organizations supported

Scenario: Statistics are empty on a new platform
	Given The platform has no logged hours
	When The user requests the public statistics
	Then The public statistics show 0 hours and 0 organizations supported

Scenario: Public statistics do not expose personal data
	Given The platform has 2 donor organizations with 10 logged hours each
	When The user requests the public statistics
	Then The public statistics contain only totals
