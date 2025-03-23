# Pre-commit Hook for Code Formatting

## Overview

This repository includes a `pre-commit` hook script located in the `git-scripts`
directory. This script ensures that code is properly formatted before each
commit, helping maintain code quality and consistency across the project.

## Purpose

The `pre-commit` hook runs the `dotnet format` command to automatically format
your code according to the project's coding standards. If the formatting fails,
the commit is aborted, and you are prompted to fix the issues before committing
again. This helps prevent unformatted code from being committed to the
repository.

## Benefits

- **Consistency**: Ensures that all code follows the same formatting rules,
  making it easier to read and maintain.
- **Quality**: Reduces the likelihood of formatting-related issues in code
  reviews.
- **Automation**: Automates the formatting process, saving time and effort for
  developers.

## Setup

To use the `pre-commit` hook in your own `.git` folder, follow these steps:

1. **Copy the `pre-commit` script**:

   Copy the `pre-commit` script from the `git-scripts` directory to your local
   `.git/hooks` directory.

   ```bash
   cp git-scripts/pre-commit .git/hooks/pre-commit
   ```

2. **Make the script executable**:

   Ensure that the `pre-commit` script is executable by running the following
   command:

   ```bash
   chmod +x .git/hooks/pre-commit
   ```

3. **Verify the setup**:

   The `pre-commit` hook is now set up and will run automatically before each
   commit. You can verify this by making a commit and observing the output.

## Conclusion

By using this `pre-commit` hook, you can ensure that your code is consistently
formatted and adheres to the project's coding standards. This helps maintain
code quality and reduces the effort required for code reviews. Add this script
to your `.git/hooks` directory to take advantage of automated code formatting in
your development workflow.