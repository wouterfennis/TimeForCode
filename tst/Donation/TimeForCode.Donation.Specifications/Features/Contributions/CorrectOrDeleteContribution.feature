Feature: Correct or delete a time contribution
	As a user who logged time on a project
	I want to correct or remove a contribution I logged
	So that the contributions and hour totals of the project stay accurate and every change remains traceable

Background:
	Given There is a published project on the time for code platform

Scenario: Contributor corrects the hours of their contribution
	Given The user has an access token
	And The user has already logged 3 hours on the project
	When The user corrects the contribution to 5 hours
	Then The contribution shows 5 hours
	And The contributions of the project add up to 5 hours

Scenario: Contributor changes the GitHub reference of their contribution
	Given The user has an access token
	And The user has already logged 3 hours on the project with the GitHub reference "https://github.com/owner/repo/pull/12"
	When The user corrects the contribution to 3 hours with the GitHub reference "https://github.com/owner/repo/pull/34"
	Then The contribution includes the GitHub reference "https://github.com/owner/repo/pull/34"
	And The contribution no longer includes the GitHub reference "https://github.com/owner/repo/pull/12"

Scenario: Contributor removes the GitHub reference of their contribution
	Given The user has an access token
	And The user has already logged 3 hours on the project with the GitHub reference "https://github.com/owner/repo/pull/12"
	When The user corrects the contribution to 3 hours without a GitHub reference
	Then The contribution no longer includes the GitHub reference "https://github.com/owner/repo/pull/12"

Scenario: A correction keeps the previous values as history
	Given The user has an access token
	And The user has already logged 3 hours on the project with the GitHub reference "https://github.com/owner/repo/pull/12"
	When The user corrects the contribution to 5 hours without a GitHub reference
	Then The history of the contribution keeps 3 hours and the GitHub reference "https://github.com/owner/repo/pull/12"
	And The correction is recorded with the user and the time

Scenario: Contributor deletes one of their contributions
	Given The user has an access token
	And The user has already logged 3 hours on the project
	And The user has already logged 2 hours on the project
	When The user deletes the contribution of 3 hours
	Then The contribution of 3 hours is no longer listed for the project
	And The contributions of the project add up to 2 hours

Scenario: A deleted contribution remains stored
	Given The user has an access token
	And The user has already logged 3 hours on the project
	When The user deletes the contribution
	Then The contribution is kept on the time for code platform as deleted
	And The deletion is recorded with the user and the time

Scenario: Administrator corrects the contribution of another user
	Given Another user has already logged 3 hours on the project
	And The user has an administrator access token
	When The user corrects the contribution to 4 hours
	Then The contribution shows 4 hours
	And The history of the contribution keeps 3 hours
	And The correction is recorded with the user and the time

Scenario: Administrator deletes the contribution of another user
	Given Another user has already logged 3 hours on the project
	And The user has an administrator access token
	When The user deletes the contribution
	Then The contribution is no longer listed for the project
	And The deletion is recorded with the user and the time

Scenario: User who is not the contributor cannot correct a contribution
	Given Another user has already logged 3 hours on the project
	And The user has an access token
	When The user corrects the contribution to 5 hours
	Then The request is rejected as forbidden
	And The contributions of the project add up to 3 hours

Scenario: User who is not the contributor cannot delete a contribution
	Given Another user has already logged 3 hours on the project
	And The user has an access token
	When The user deletes the contribution
	Then The request is rejected as forbidden
	And The contributions of the project add up to 3 hours

Scenario: Correcting a contribution requires authentication
	Given The user has already logged 3 hours on the project
	And The user does not have an access token
	When The user corrects the contribution to 5 hours
	Then The request is rejected as unauthorized

Scenario: Deleting a contribution requires authentication
	Given The user has already logged 3 hours on the project
	And The user does not have an access token
	When The user deletes the contribution
	Then The request is rejected as unauthorized

Scenario: User attempts to correct a contribution that does not exist
	Given The user has an access token
	And There is no contribution with the given identifier
	When The user corrects the contribution to 5 hours
	Then The request is rejected as not found

Scenario: User attempts to delete a contribution that does not exist
	Given The user has an access token
	And There is no contribution with the given identifier
	When The user deletes the contribution
	Then The request is rejected as not found

Scenario: User attempts to correct a contribution that is already deleted
	Given The user has an access token
	And The user has already logged 3 hours on the project
	And The user has already deleted the contribution
	When The user corrects the contribution to 5 hours
	Then The request is rejected as not found

Scenario: User attempts to delete a contribution that is already deleted
	Given The user has an access token
	And The user has already logged 3 hours on the project
	And The user has already deleted the contribution
	When The user deletes the contribution
	Then The request is rejected as not found

Scenario Outline: User attempts to correct a contribution to an amount of hours that is not positive
	Given The user has an access token
	And The user has already logged 3 hours on the project
	When The user corrects the contribution to <hours> hours
	Then The request is rejected as invalid
	And The contributions of the project add up to 3 hours

	Examples:
		| hours |
		| 0     |
		| -2    |

Scenario Outline: User attempts to correct a contribution with a reference that is not a GitHub link
	Given The user has an access token
	And The user has already logged 3 hours on the project with the GitHub reference "https://github.com/owner/repo/pull/12"
	When The user corrects the contribution to 3 hours with the GitHub reference "<reference>"
	Then The request is rejected as invalid
	And The contribution includes the GitHub reference "https://github.com/owner/repo/pull/12"

	Examples:
		| reference                              |
		| not-a-link                             |
		| http://github.com/owner/repo/pull/12   |
		| https://example.com/owner/repo/pull/12 |

Scenario: Deleting a contribution gives its hours back to the donation
	Given The user has an active donation of 5 hours
	And The user has an access token
	And The user has logged 5 hours that count towards the donation
	And The donation is completed
	When The user deletes the contribution
	Then The donation is active
	And The donation shows 0 hours logged

Scenario: Lowering the hours of a contribution gives the difference back to the donation
	Given The user has an active donation of 5 hours
	And The user has an access token
	And The user has logged 5 hours that count towards the donation
	When The user corrects the contribution to 3 hours
	Then The donation is active
	And The donation shows 3 hours logged

Scenario: Changing only the GitHub reference leaves the donation as it was
	Given The user has an active donation of 5 hours
	And The user has an access token
	And The user has logged 5 hours that count towards the donation
	When The user corrects the contribution to 5 hours without a GitHub reference
	Then The donation is completed
	And The donation shows 5 hours logged

Scenario: Raising the hours of a contribution counts the extra hours toward the donation
	Given The user has an active donation of 5 hours
	And The user has an access token
	And The user has logged 3 hours that count towards the donation
	When The user corrects the contribution to 5 hours
	Then The donation is completed
	And The donation shows 5 hours logged