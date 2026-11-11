Feature: Organization badges
	As a donor organization
	I want to be awarded a badge when we cross an hour milestone
	So that our contribution is recognised and we can share it publicly

Scenario: Organization earns a badge when it reaches a threshold
	Given There is an organization badge tier "Bronze" at 10 hours
	And There is a donor organization on the time for code platform
	And The donor organization has an active donation of 20 hours
	And The user has logged 10 hours that count towards the donation
	When The user requests the badges of the donor organization
	Then The organization has the badge "Bronze"

Scenario: Organization below the threshold has no badge
	Given There is an organization badge tier "Bronze" at 10 hours
	And There is a donor organization on the time for code platform
	And The donor organization has an active donation of 20 hours
	And The user has logged 9 hours that count towards the donation
	When The user requests the badges of the donor organization
	Then The organization has no badges

Scenario: Organization crossing several thresholds earns every tier
	Given There is an organization badge tier "Bronze" at 10 hours
	And There is an organization badge tier "Silver" at 20 hours
	And There is a donor organization on the time for code platform
	And The donor organization has an active donation of 30 hours
	And The user has logged 25 hours that count towards the donation
	When The user requests the badges of the donor organization
	Then The organization has the badge "Bronze"
	And The organization has the badge "Silver"

Scenario: A badge is awarded only once per tier
	Given There is an organization badge tier "Bronze" at 10 hours
	And There is a donor organization on the time for code platform
	And The donor organization has an active donation of 30 hours
	And The user has logged 12 hours that count towards the donation
	And The user has requested the badges of the donor organization
	And The user has logged 5 more hours that count towards the donation
	When The user requests the badges of the donor organization
	Then The organization has the badge "Bronze" exactly once

Scenario: Hours logged at the same time award a badge only once
	Given There is an organization badge tier "Bronze" at 10 hours
	And There is a donor organization on the time for code platform
	And The donor organization has an active donation of 30 hours
	And The user has logged 12 hours that count towards the donation
	When The badges of the donor organization are requested by 5 visitors at the same time
	Then The organization has the badge "Bronze" exactly once

Scenario: A tier added later is awarded to an organization that already qualifies
	Given There is a donor organization on the time for code platform
	And The donor organization has an active donation of 30 hours
	And The user has logged 12 hours that count towards the donation
	And There is an organization badge tier "Bronze" at 10 hours
	When The user requests the badges of the donor organization
	Then The organization has the badge "Bronze"

Scenario: Badges are public
	Given There is an organization badge tier "Bronze" at 10 hours
	And There is a donor organization on the time for code platform
	And The donor organization has an active donation of 20 hours
	And The user has logged 10 hours that count towards the donation
	And The user does not have an access token
	When The user requests the badges of the donor organization
	Then The organization has the badge "Bronze"

Scenario: Badges of an unknown organization are not found
	Given There is no donor organization with the given identifier
	When The user requests the badges of the donor organization
	Then The request is rejected as not found
