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
	Then The request is rejected as invalid

	Examples:
		| repository state |
		| private          |
		| archived         |

Scenario: User attempts to publish the same repository twice
	Given The user has an access token
	And The user has already published the repository on the time for code platform
	When The user publishes the repository on the time for code platform
	Then The request is rejected as a conflict

Scenario: Publishing a repository requires authentication
	Given The user does not have an access token
	When The user publishes the repository on the time for code platform
	Then The request is rejected as unauthorized

Scenario: User attempts to publish a repository that does not exist on the external platform
	Given The user has an access token
	And The external platform does not have the repository
	When The user publishes the repository on the time for code platform
	Then The request is rejected as invalid


@live
@liveonly
Scenario: A new repository is published with an access token issued by the authorization service
	Given The user has an access token
	When The user publishes a new repository on the live platform
	Then The project is registered on the time for code platform
	And The project is in draft on the live platform

@live
@liveonly
Scenario: Publishing the same repository twice is rejected
	Given The user has an access token
	And The user has published a new repository on the live platform
	When The user publishes the same repository on the live platform again
	Then The request is rejected as a conflict