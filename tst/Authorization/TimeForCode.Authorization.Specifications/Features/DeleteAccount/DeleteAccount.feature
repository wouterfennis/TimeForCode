Feature: Delete account
	As a user
	I want to delete my account and personal data from the time for code platform
	So that the platform no longer holds or shows my personal data

Scenario: User deletes their account
	Given The user has an access token
	And The user has a refresh token
	And The user has a profile on the time for code platform
	When The user deletes their account
	Then The account deletion is confirmed
	And The profile of the user is removed
	And The refresh token is revoked

Scenario: Deleted user can no longer retrieve their profile
	Given The user has an access token
	And The user has a profile on the time for code platform
	And The user has deleted their account
	When The user requests their profile from the time for code platform
	Then The user is informed their profile was not found

Scenario: Deleted user can no longer refresh their token
	Given The user has a refresh token
	And The user has a profile on the time for code platform
	And The user has deleted their account
	When The user calls the refresh token endpoint
	Then The following refresh error message is returned: "Token refresh failed."

Scenario: Deleting an account anonymises the user in the donation module
	Given The user has an access token
	And The user has a profile on the time for code platform
	When The user deletes their account
	Then The donation module is asked to anonymise the contributor of the user

Scenario: Account is kept when the donation module cannot anonymise the contributor
	Given The user has an access token
	And The user has a profile on the time for code platform
	And The donation module cannot anonymise the contributor of the user
	When The user deletes their account
	Then The user is informed the account could not be deleted
	And The profile of the user is kept

Scenario: Deleting an account does not affect other users
	Given The user has an access token
	And The user has a profile on the time for code platform
	And Another user has a profile on the time for code platform
	When The user deletes their account
	Then The profile of the other user is kept

Scenario: User without a profile deletes their account
	Given The user has an access token
	And The user does not have a profile on the time for code platform
	When The user deletes their account
	Then The user is informed their profile was not found

Scenario: Deleting an account requires authentication
	Given The user has not logged in at the time for code platform
	When The user deletes their account
	Then The user is informed the account deletion requires authentication
