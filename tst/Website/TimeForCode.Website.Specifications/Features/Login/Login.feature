@E2E
Feature: Login
    As a user
    I want to log in via GitHub
    So that I can access authenticated features of the platform

Scenario: User completes OAuth login through the Identity Provider Mock
    When The visitor navigates to the home page
    And The visitor clicks the login link
    And The OAuth flow completes through the identity provider mock
    Then The visitor is returned to the home page
    And The user name is visible
    And The logout link is visible
