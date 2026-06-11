Feature: Home Page
    As a visitor
    I want to see the home page
    So that I can discover published projects and log in

Scenario: Unauthenticated visitor sees login link and published projects section
    When The visitor navigates to the home page
    Then The login link is visible
    And The published projects section is visible
