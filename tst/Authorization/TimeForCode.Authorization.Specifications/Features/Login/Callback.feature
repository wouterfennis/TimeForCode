Feature: Callback
	As a user
	I want to login with my existing account from another platform
	So that my data is shared between the platforms

Login consists technically on two parts.
1. The initial call to the authorization service where a redirect url towards the external platform is returned.
2. The user is redirected to the external platform to perform the authentication.
3. The external platform calls the time for code platform to complete the authorization
4. The time for code platform calls the external platform to receive the access token

Scenario: User has not logged in at the external platform
	Given The user has not logged in at the external platform
	When The external platform calls the time for code platform to complete the authorization
	Then An access token is not returned
	And The following callback error message is returned: "State is not known"

Scenario: Access token cannot be received at the external platform
	Given The user logs in at the time for code platform
	And The external platform does not return the access token
	When The external platform calls the time for code platform to complete the authorization
	Then An access token is not returned
	And The following callback error message is returned: "Access token cannot be received"

Scenario: User has no account information at the external platform
	Given The user has no account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform
	When The external platform calls the time for code platform to complete the authorization
	Then An access token is not returned
	And The following callback error message is returned: "No account information"

Scenario: User has an account at the external platform
	Given The user has an account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform
	When The external platform calls the time for code platform to complete the authorization
	Then An access token is returned
	And The user information is saved in the time for code platform
