Feature: Admin Registration
	As the time for code platform
	I want to allow exactly one device to permanently claim the admin role using a WebAuthn passkey
	So that a single trusted operator can administer the platform without a traditional password

Background:
	Given The time for code platform has no registered admin credential

Scenario: The user registers the first admin passkey with a valid bootstrap secret
	When The user registers an admin passkey with the correct bootstrap secret
	Then The admin passkey registration succeeds
	And The time for code platform permanently claims the admin role for the user's device

Scenario: The user registers an admin passkey with an incorrect bootstrap secret
	When The user registers an admin passkey with an incorrect bootstrap secret
	Then The admin passkey registration is rejected
	And The time for code platform still has no registered admin credential

Scenario: A second device attempts to register after the admin role has already been claimed
	Given The time for code platform already has a registered admin credential
	When The user registers an admin passkey with the correct bootstrap secret
	Then The admin passkey registration is rejected
	And The time for code platform still has only the original registered admin credential

Scenario: Two devices attempt to register the admin passkey at the same time
	When Two devices simultaneously register an admin passkey with the correct bootstrap secret
	Then Only one of the two admin passkey registrations succeeds
	And The time for code platform has exactly one registered admin credential
