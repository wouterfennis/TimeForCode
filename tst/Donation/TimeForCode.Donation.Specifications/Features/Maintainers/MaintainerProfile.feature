Feature: Maintainer profile and project ownership
	As a maintainer
	I want the platform to keep a maintainer profile for me and show me my own projects
	So that I can follow the status and received donations of my projects and only I can manage them

Scenario: Publishing the first project creates the maintainer profile
	Given The user has an access token
	And The user is not yet a maintainer on the time for code platform
	And The repository is public and active on the external platform
	When The user publishes the repository on the time for code platform
	Then The user is registered as a maintainer on the time for code platform

Scenario: Publishing a further project reuses the maintainer profile
	Given The user has an access token
	And The user is registered as a maintainer on the time for code platform
	And The repository is public and active on the external platform
	When The user publishes the repository on the time for code platform
	Then The user is registered as one maintainer on the time for code platform

Scenario: Maintainer views their own maintainer profile
	Given The user has an access token
	And The user is registered as a maintainer on the time for code platform
	When The user requests their own maintainer profile
	Then The maintainer profile of the user is returned

Scenario: User without a maintainer profile requests it
	Given The user has an access token
	And The user is not yet a maintainer on the time for code platform
	When The user requests their own maintainer profile
	Then The request is rejected as not found

Scenario: Maintainer lists their own projects with status and received donations
	Given The user has an access token
	And The user has a project that is in draft
	And The user has an active project with donations of 10 and 5 hours
	When The user requests their own projects
	Then The projects of the user are returned
	And The user sees the project in draft
	And The user sees the active project with 15 hours donated

Scenario: Projects of other maintainers are not listed
	Given The user has an access token
	And The user has a project that is active
	And There is a project published by another user
	When The user requests their own projects
	Then Only the projects of the user are returned

Scenario: Maintainer without projects gets an empty list
	Given The user has an access token
	And The user is registered as a maintainer on the time for code platform
	When The user requests their own projects
	Then An empty list of projects is returned

Scenario Outline: Only the owning maintainer can change a project
	Given The user has an access token
	And There is a project published by another user
	When The user <action> the project
	Then The request is rejected as forbidden

	Examples:
		| action             |
		| submits for review |
		| archives           |
		| re-activates       |

Scenario: Viewing the maintainer profile requires authentication
	Given The user does not have an access token
	When The user requests their own maintainer profile
	Then The request is rejected as unauthorized

Scenario: Listing own projects requires authentication
	Given The user does not have an access token
	When The user requests their own projects
	Then The request is rejected as unauthorized
