@E2E
Feature: Projects List
    As a visitor
    I want to browse published projects
    So that I can discover open-source projects to support

Scenario: Visitor sees at least one project tile that links to the project detail page
    When The visitor navigates to the projects page
    Then At least one project tile is visible
    And Each project tile links to the project detail page
