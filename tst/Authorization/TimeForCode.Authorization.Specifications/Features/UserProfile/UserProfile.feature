Feature: User profile
	As a user
	I want to retrieve my profile information from the time for code platform
	So that I can view my GitHub profile data in one place

Scenario: Authenticated user retrieves their profile
	Given The user has an account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform
	And The external platform calls the time for code platform to complete the authorization
	And The user has an access token
	When The user requests their profile from the time for code platform
	Then The profile is returned with name, login, avatar, email, company, bio, and location

Scenario: User profile is not found
	Given The user has an access token
	And The user does not have a profile on the time for code platform
	When The user requests their profile from the time for code platform
	Then The user is informed their profile was not found
