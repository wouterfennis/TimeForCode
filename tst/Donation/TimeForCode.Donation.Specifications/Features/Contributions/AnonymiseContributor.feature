Feature: Anonymise contributor
	As a user who has deleted their account
	I want my identity removed from my logged contributions
	So that my hours remain on the projects without showing who I am

Scenario: Contributor is shown as a deleted user after account deletion
	Given There is a published project on the time for code platform
	And The user has already logged 3 hours on the project
	When The account of the user is deleted at the time for code platform
	Then The contributor of the user is shown as a deleted user
	And The contributions of the project no longer show the login or avatar of the user

Scenario: Hours of an anonymised contributor are kept
	Given There is a published project on the time for code platform
	And The user has already logged 3 hours on the project
	And The user has already logged 2 hours on the project
	When The account of the user is deleted at the time for code platform
	Then 2 contributions are returned
	And The contributions of the project add up to 5 hours

Scenario: Other contributors are not anonymised
	Given There is a published project on the time for code platform
	And The user has already logged 3 hours on the project
	And Another user has already logged 2 hours on the project
	When The account of the user is deleted at the time for code platform
	Then The contributor of the other user is unchanged

Scenario: User without contributions can be anonymised
	When The account of a user without contributions is deleted at the time for code platform
	Then The anonymisation is confirmed

Scenario: Anonymising the same contributor twice is accepted
	Given The user has already logged 3 hours on a published project
	And The account of the user has been deleted at the time for code platform
	When The account of the user is deleted at the time for code platform
	Then The anonymisation is confirmed

Scenario: A regular user cannot anonymise a contributor
	Given The user has an access token
	When The user asks to anonymise a contributor
	Then The request is rejected as forbidden
