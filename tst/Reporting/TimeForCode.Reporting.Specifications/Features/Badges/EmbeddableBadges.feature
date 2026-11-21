Feature: Embeddable README badges
	As a maintainer, donor organization or contributor
	I want to show my TimeForCode standing as a badge image in a README
	So that visitors see my support and recognition without a login, and the badge keeps itself up to date

Scenario: Project badge shows the hours received
	Given There is a project on the time for code platform
	And The project has a donation of 10 hours with status "active"
	When The user requests the hours received badge of the project
	Then The badge shows "10 hours"

Scenario: Project progress badge shows hours logged out of hours received
	Given There is a project on the time for code platform
	And The project has a donation of 10 hours with status "active"
	And Contributor "alice" has logged 4 hours on the project
	When The user requests the progress badge of the project
	Then The badge shows "4 / 10 hours (40%)"

Scenario: Unpublished project shows a not found badge
	Given There is a project on the time for code platform
	And The project is not published
	When The user requests the hours received badge of the project
	Then The badge shows "not found"

Scenario: Unknown project shows a not found badge
	Given There is no project with the given identifier
	When The user requests the hours received badge of the project
	Then The badge shows "not found"

Scenario: Malformed project identifier shows a not found badge
	Given The project identifier is "not-an-identifier"
	When The user requests the hours received badge of the project
	Then The badge shows "not found"

Scenario: Organization badge shows its highest tier
	Given There is a donor organization on the time for code platform
	And There is an organization badge tier "Silver" at 10 hours
	And There is an organization badge tier "Gold" at 20 hours
	And The donor organization has an active donation of 25 hours
	And The user has logged 25 hours that count towards the donation
	When The user requests the badge of the donor organization
	Then The badge shows "Gold"

Scenario: Organization without a badge shows a no badge yet badge
	Given There is a donor organization on the time for code platform
	And There is an organization badge tier "Silver" at 10 hours
	When The user requests the badge of the donor organization
	Then The badge shows "no badge yet"

Scenario: Unknown organization shows a not found badge
	Given There is no donor organization with the given identifier
	When The user requests the badge of the donor organization
	Then The badge shows "not found"

Scenario: Contributor badge shows the highest tier
	Given There is a project on the time for code platform
	And There is a contributor badge tier "Bronze" at 10 hours
	And There is a contributor badge tier "Silver" at 20 hours
	And Contributor "Alice" has logged 25 hours on the project
	When The user requests the badge of Contributor "Alice"
	Then The badge shows "Silver"

Scenario: Contributor without a badge shows a no badge yet badge
	Given There is a project on the time for code platform
	And There is a contributor badge tier "Bronze" at 10 hours
	And Contributor "Alice" has logged 9 hours on the project
	When The user requests the badge of Contributor "Alice"
	Then The badge shows "no badge yet"

Scenario: Unknown contributor looks exactly like a contributor without a badge
	Given There is a project on the time for code platform
	And There is a contributor badge tier "Bronze" at 10 hours
	And Contributor "Alice" has logged 9 hours on the project
	And There is no contributor with the given identifier
	When The user requests the badge of the unknown contributor
	Then The badge is identical to the badge of Contributor "Alice"

Scenario: Badges can be cached by image proxies
	Given There is a project on the time for code platform
	And The project has a donation of 10 hours with status "active"
	When The user requests the hours received badge of the project
	Then The badge may be cached by others for a limited time

Scenario: Badge shows an unavailable badge when donation data cannot be retrieved
	Given There is a project on the time for code platform
	And The donation data is unavailable
	When The user requests the hours received badge of the project
	Then The badge shows "unavailable"
	And The badge may be cached by others only for a short time

Scenario: Badges are rate limited for visitors who request too many
	Given There is a project on the time for code platform
	When The badge of the project is requested by more visitors than the public limit allows
	Then The request is rejected as too many requests

Scenario: Badge text is escaped
	Given There is a donor organization on the time for code platform
	And There is an organization badge tier "<Gold & Co>" at 10 hours
	And The donor organization has an active donation of 10 hours
	And The user has logged 10 hours that count towards the donation
	When The user requests the badge of the donor organization
	Then The badge shows "<Gold & Co>"

Scenario: Badge shows only totals, tier names and counts
	Given There is a project on the time for code platform
	And The project has a donation of 10 hours with status "active"
	And Contributor "alice" has logged 4 hours on the project
	When The user requests the progress badge of the project
	Then The badge does not expose contributor "alice"
