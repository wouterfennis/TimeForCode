Feature: Badge tier configuration
	As an administrator
	I want to define the badge tiers and their hour thresholds
	So that milestone recognition can be tuned without a new release

Scenario: Administrator defines a badge tier
	Given The user has an administrator access token
	When The user defines the organization badge tier "Bronze" at 10 hours
	Then The organization badge tier "Bronze" is listed with a threshold of 10 hours

Scenario: Administrator changes the threshold of a badge tier
	Given The user has an administrator access token
	And There is an organization badge tier "Bronze" at 10 hours
	When The user defines the organization badge tier "Bronze" at 20 hours
	Then The organization badge tier "Bronze" is listed with a threshold of 20 hours

Scenario: Administrator removes a badge tier
	Given The user has an administrator access token
	And There is an organization badge tier "Bronze" at 10 hours
	When The user removes the organization badge tier "Bronze"
	Then The organization badge tier "Bronze" is no longer listed

Scenario: Tier thresholds must be positive
	Given The user has an administrator access token
	When The user defines the organization badge tier "Bronze" at 0 hours
	Then The request is rejected as invalid

Scenario: Two tiers cannot share a threshold
	Given The user has an administrator access token
	And There is an organization badge tier "Bronze" at 10 hours
	When The user defines the organization badge tier "Silver" at 10 hours
	Then The request is rejected as a conflict

Scenario: Regular user cannot configure badge tiers
	Given The user has an access token
	When The user defines the organization badge tier "Bronze" at 10 hours
	Then The request is rejected as forbidden

Scenario: Anonymous visitor cannot configure badge tiers
	Given The user does not have an access token
	When The user defines the organization badge tier "Bronze" at 10 hours
	Then The request is rejected as unauthorized

Scenario: Organization and contributor tiers are independent
	Given The user has an administrator access token
	And There is an organization badge tier "Bronze" at 10 hours
	When The user defines the contributor badge tier "Bronze" at 5 hours
	Then The contributor badge tier "Bronze" is listed with a threshold of 5 hours
	And The organization badge tier "Bronze" is listed with a threshold of 10 hours

Scenario: Tiers of different recipient types may share a threshold
	Given The user has an administrator access token
	And There is an organization badge tier "Bronze" at 10 hours
	When The user defines the contributor badge tier "Starter" at 10 hours
	Then The contributor badge tier "Starter" is listed with a threshold of 10 hours
