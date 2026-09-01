@E2E
Feature: Admin Access
	As the time for code platform
	I want to restrict the admin landing page to authenticated admins
	So that only the claimed admin device can manage the platform

Scenario: An unauthenticated visitor navigates to the admin landing page
	When The visitor navigates to the admin landing page
	Then The visitor sees an unauthorized message

Scenario: An authenticated admin navigates to the admin landing page
	Given The user is logged in as admin
	When The visitor navigates to the admin landing page
	Then The admin landing page is visible

Scenario: The admin logs out and loses access to the admin landing page
	Given The user is logged in as admin
	When The visitor triggers logout
	And The visitor navigates to the admin landing page
	Then The visitor sees an unauthorized message
