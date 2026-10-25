Feature: User repositories
	As a user
	I want to browse my public GitHub repositories from the time for code platform
	So that my GitHub activity is visible on my profile

Scenario: Authenticated user retrieves their public repositories
	Given The user has an account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform
	And The external platform calls the time for code platform to complete the authorization
	And The user has an access token
	When The user requests their repositories from the time for code platform
	Then A list of public repositories is returned with name, description, star count, language, and URL

Scenario: Authenticated user retrieves a single repository by owner and name
	Given The user has an account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform
	And The external platform calls the time for code platform to complete the authorization
	And The user has an access token
	When The user requests repository "johndoe/my-repo" from the time for code platform
	Then A single repository is returned with name, description, star count, language, and URL

Scenario Outline: Requesting a repository with an invalid owner or name is rejected
	Given The user has an access token
	When The user requests repository "<repository>" from the time for code platform
	Then The request is rejected as invalid

	Examples:
		| repository                                                                                             |
		| bad_owner/my-repo                                                                                      |
		| -johndoe/my-repo                                                                                       |
		| johndoe-/my-repo                                                                                       |
		| aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/my-repo                                                       |
		| johndoe/bad$name                                                                                       |
		| johndoe%0A/my-repo                                                                                  |
		| johndoe/my-repo%0A                                                                                  |
		| johndoe/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa |
