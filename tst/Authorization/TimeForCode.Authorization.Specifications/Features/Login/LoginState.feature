Feature: Login state
	As a user
	I want my login attempt to stay valid only for the one login I started
	So that nobody can reuse or intercept my login

Login state links the start of a login to its completion at the callback.
It must survive across instances and restarts, expire, and be usable only once.

Scenario: Login completes on another instance of the time for code platform
	Given The user has an account at the external platform
	And The user logs in at one instance of the time for code platform
	And The user logs in at the external platform
	When The external platform calls another instance of the time for code platform to complete the authorization
	Then An access token is returned

Scenario: Login completes after the time for code platform restarted
	Given The user has an account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform
	And The time for code platform has restarted
	When The external platform calls the time for code platform to complete the authorization
	Then An access token is returned

Scenario: Login state cannot be used a second time
	Given The user has an account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform
	And The external platform has already completed the authorization
	When The external platform calls the time for code platform to complete the authorization
	Then An access token is not returned
	And The following callback error message is returned: "Authentication failed."

Scenario: Login state expires after 10 minutes
	Given The user has an account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform
	And More than 10 minutes have passed since the user logged in at the time for code platform
	When The external platform calls the time for code platform to complete the authorization
	Then An access token is not returned
	And The following callback error message is returned: "Authentication failed."

Scenario: Login is protected with a code challenge
	Given The user has an account at the external platform
	When The user logs in at the time for code platform
	Then The user is redirected to the external platform
	And The redirect to the external platform contains a code challenge

Scenario: Access token request proves the login started at the time for code platform
	Given The user has an account at the external platform
	And The user logs in at the time for code platform
	And The user logs in at the external platform
	When The external platform calls the time for code platform to complete the authorization
	Then An access token is returned
	And The time for code platform proved to the external platform that it started the login
