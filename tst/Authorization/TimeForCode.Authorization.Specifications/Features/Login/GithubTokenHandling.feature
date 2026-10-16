Feature: GitHub token handling
	As a user
	I want the time for code platform to ask for and keep as little of my GitHub access as needed
	So that my GitHub account is not exposed if the platform's data leaks

Scenario: Login requests only public profile access
	Given The user has an account at the external platform
	When The user logs in at the time for code platform
	Then The user is redirected to the external platform
	And The redirect does not request profile write or email access

Scenario: The external platform's access token is not kept after login
	Given The user has an account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform
	When The external platform calls the time for code platform to complete the authorization
	Then An access token is returned
	And The user information is saved in the time for code platform
	And No access token of the external platform is stored for the user

Scenario: Public repositories are listed without a stored user token
	Given The user has an access token
	And The user has no stored access token of the external platform
	When The user requests their repositories from the time for code platform
	Then A list of public repositories is returned with name, description, star count, language, and URL

Scenario: Private repositories are never listed
	Given The user has an access token
	And The user owns a private repository at the external platform
	When The user requests their repositories from the time for code platform
	Then The private repository is not in the returned list

Scenario: A single public repository is returned without a stored user token
	Given The user has an access token
	And The user has no stored access token of the external platform
	When The user requests repository "johndoe/my-repo" from the time for code platform
	Then A single repository is returned with name, description, star count, language, and URL

Scenario: A private repository cannot be requested
	Given The user has an access token
	And The user owns a private repository at the external platform
	When The user requests repository "johndoe/secret-repo" from the time for code platform
	Then The user is informed the repository was not found
