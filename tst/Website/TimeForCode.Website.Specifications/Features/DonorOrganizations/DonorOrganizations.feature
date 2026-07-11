@E2E
Feature: Donor Organizations Admin
	As a user
	I want to manage donor organizations
	So that I can maintain the list of organizations donating developer time

Scenario: Visitor navigates to the donor organizations admin page
	When The visitor navigates to the donor organizations page
	Then The donor organizations heading is visible
	And The add donor organization button is visible

Scenario: Visitor opens the add donor organization form
	When The visitor navigates to the donor organizations page
	And The visitor clicks the add donor organization button
	Then The donor organization form is visible

Scenario: Visitor cancels the add donor organization form
	When The visitor navigates to the donor organizations page
	And The visitor clicks the add donor organization button
	And The visitor cancels the donor organization form
	Then The donor organization form is not visible
	And The add donor organization button is visible
