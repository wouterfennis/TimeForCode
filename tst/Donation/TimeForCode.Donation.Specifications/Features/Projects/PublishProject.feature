Feature: Publish a project
	As a user
	I want to publish a GitHub repository as a project on the time for code platform
	So that other contributors can discover my work and choose to support it

Scenario: Authenticated user publishes a public repository
	Given The user has an access token
	And The repository is public and active on the external platform
	When The user publishes the repository on the time for code platform
	Then The project is registered on the time for code platform

Scenario: Published project stores the full GitHub metadata snapshot
	Given The user has an access token
	And The repository is public and active on the external platform
	When The user publishes the repository on the time for code platform
	Then The stored project metadata includes the following fields:
		| Field             |
		| name              |
		| full_name         |
		| description       |
		| html_url          |
		| language          |
		| topics            |
		| stargazers_count  |
		| forks_count       |
		| open_issues_count |
		| homepage          |
		| default_branch    |
		| license           |
		| owner login       |
		| owner avatar url  |
		| created_at        |
		| updated_at        |
		| pushed_at         |

Scenario Outline: User attempts to publish a repository that cannot be made public
	Given The user has an access token
	And The repository is <repository state> on the external platform
	When The user publishes the repository on the time for code platform
	Then The user is informed the repository cannot be published

	Examples:
		| repository state |
		| private          |
		| archived         |

Scenario: User attempts to publish the same repository twice
	Given The user has an access token
	And The user has already published the repository on the time for code platform
	When The user publishes the repository on the time for code platform
	Then The user is informed the repository is already published

Scenario: User republishes a previously unpublished repository
	Given The user has an access token
	And The repository is public and active on the external platform
	And The user has previously unpublished the repository on the time for code platform
	When The user publishes the repository on the time for code platform
	Then The project is registered on the time for code platform

Scenario: Publishing a repository requires authentication
	Given The user does not have an access token
	When The user publishes the repository on the time for code platform
	Then The user is informed they must be logged in

Scenario: User attempts to publish a repository that does not exist on the external platform
	Given The user has an access token
	And The external platform does not have the repository
	When The user publishes the repository on the time for code platform
	Then The user is informed the repository cannot be published
