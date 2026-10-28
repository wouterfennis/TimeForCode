Feature: Log a time contribution
	As a user
	I want to log the hours I spent on a published project
	So that my contribution to open source work is recorded and visible to others

Scenario: Authenticated user logs hours on a published project
	Given The user has an access token
	And There is a published project on the time for code platform
	When The user logs 3 hours on the project
	Then The contribution is recorded on the time for code platform
	And The contribution shows 3 hours
	And The contributions of the project add up to 3 hours
	And The contribution shows the GitHub login and avatar of the user

Scenario: User logs hours with a GitHub reference as evidence
	Given The user has an access token
	And There is a published project on the time for code platform
	When The user logs 3 hours on the project with the GitHub reference "https://github.com/owner/repo/pull/12"
	Then The contribution is recorded on the time for code platform
	And The contribution shows 3 hours
	And The contribution includes the GitHub reference "https://github.com/owner/repo/pull/12"

Scenario: First contribution registers the user as a contributor
	Given The user has an access token
	And There is a published project on the time for code platform
	And The user is not yet a contributor on the time for code platform
	When The user logs 3 hours on the project
	Then The user is registered as a contributor on the time for code platform

Scenario: Logging a second contribution adds to the hours already logged
	Given The user has an access token
	And There is a published project on the time for code platform
	And The user has already logged 3 hours on the project
	When The user logs 2 hours on the project
	Then The contribution shows 2 hours
	And The contributions of the project add up to 5 hours
	And The user is registered as one contributor on the time for code platform
	And Both contributions are linked to that contributor

Scenario: Logging a contribution requires authentication
	Given The user does not have an access token
	And There is a published project on the time for code platform
	When The user logs 3 hours on the project
	Then The request is rejected as unauthorized

Scenario: User attempts to log hours on a project that does not exist
	Given The user has an access token
	And There is no project with the given identifier
	When The user logs 3 hours on the project
	Then The request is rejected as not found

Scenario Outline: User attempts to log hours with a reference that is not a GitHub link
	Given The user has an access token
	And There is a published project on the time for code platform
	When The user logs 3 hours on the project with the GitHub reference "<reference>"
	Then The request is rejected as invalid
	And No hours are logged on the project

	Examples:
		| reference                              |
		| not-a-link                             |
		| http://github.com/owner/repo/pull/12   |
		| https://example.com/owner/repo/pull/12 |

Scenario Outline: User attempts to log an amount of hours that is not positive
	Given The user has an access token
	And There is a published project on the time for code platform
	When The user logs <hours> hours on the project
	Then The request is rejected as invalid
	And No hours are logged on the project

	Examples:
		| hours |
		| 0     |
		| -2    |

@live
@liveonly
Scenario: Logged hours are stored and listed for the project
	Given The user has an access token
	And The user has an active project registered on the live platform
	When The user logs 3 hours on the project
	And The user requests the contributions of the project
	Then The contributions of the project add up to 3 hours on the live platform

@live
@liveonly
Scenario: Logging hours on a registered project requires authentication
	Given The user has an access token
	And The user has an active project registered on the live platform
	And The user does not have an access token
	When The user logs 3 hours on the project
	Then The request is rejected as unauthorized