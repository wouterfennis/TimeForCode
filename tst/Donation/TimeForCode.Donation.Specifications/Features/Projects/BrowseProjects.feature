Feature: Browse published projects
	As a user
	I want to browse projects published on the time for code platform
	So that I can discover open source work to contribute to or support

Scenario: User browses the public projects listing
	Given There are published projects on the time for code platform
	When The user requests the list of published projects
	Then A paginated list of projects is returned
	And Each project includes a link to the original GitHub repository

Scenario: Browsing the projects listing does not require authentication
	Given There are published projects on the time for code platform
	When The user requests the list of published projects without an account
	Then A paginated list of projects is returned

Scenario: User views a project detail page
	Given There is a published project on the time for code platform
	When The user requests the project details
	Then The full project details are returned
	And The project includes a link to the original GitHub repository

Scenario: Viewing project details does not require authentication
	Given There is a published project on the time for code platform
	When The user requests the project details without an account
	Then The full project details are returned

Scenario: Archived projects do not appear in the public listing
	Given There are published projects on the time for code platform
	And One of the projects has been unpublished
	When The user requests the list of published projects
	Then The archived project is not included in the results

Scenario: Project listing includes contribution metrics
	Given There are published projects on the time for code platform
	When The user requests the list of published projects
	Then A paginated list of projects is returned
	And Each project summary includes stars, forks, and open issues counts
