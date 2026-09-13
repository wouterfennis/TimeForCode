@E2E
Feature: Admin Login
	As the user who administers the time for code platform
	I want to log in using my passkey from the home page
	So that I can access administrative features

Scenario: Visitor sees the admin login entry point on the home page
	When The visitor navigates to the home page
	Then The admin login link is visible

Scenario: The registered admin device logs in via the home page
	Given The time for code platform already has a registered admin credential for this device
	When The visitor navigates to the home page
	And The visitor clicks the admin login link
	And The user authenticates with the registered admin passkey
	Then The visitor is redirected to the admin landing page
	And The admin navigation is visible
