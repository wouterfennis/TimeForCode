Feature: Admin Authentication
	As the user who has claimed the admin role
	I want to authenticate with my registered passkey
	So that I can access administrative features without a password

Background:
	Given The time for code platform already has a registered admin credential

Scenario: The registered device authenticates as admin
	When The user logs in as admin with the registered device
	Then The admin login succeeds
	And An admin session token is issued containing the admin role and the admin scope

Scenario: A device other than the registered device attempts to authenticate as admin
	When The user logs in as admin with a device that is not the registered device
	Then The admin login is rejected
	And No admin session token is issued

Scenario: The admin logs out
	Given The user has an admin session token
	And The user has a refresh token
	When The user logs out from the external platform
	Then The logout is confirmed
	And The admin session token is no longer valid
