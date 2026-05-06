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

Scenario: GitHub access token has been revoked
	Given The user has an access token
	And The user's GitHub access token has been revoked at the external platform
	When The user requests their repositories from the time for code platform
	Then The user is informed they must re-authenticate via GitHub
