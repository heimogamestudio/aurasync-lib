# AuraSync Package Distribution Guide

This guide provides instructions for distributing the AuraSync Unity package.

## Methods of Distribution

You can distribute your Unity package using several methods:

### 1. Local Package Distribution

To create a local package:

1. Make sure your package has a valid structure and a `package.json` file
2. The easiest way is to simply reference the local folder:

   Add to your project's `Packages/manifest.json`:

   ```json
   "com.heimo.aurasync": "file:../relative/path/to/com.heimo.aurasync"
   ```

   Or use an absolute path (Windows example):

   ```json
   "com.heimo.aurasync": "file:C:/path/to/com.heimo.aurasync"
   ```

3. Alternatively, create a `.tgz` file:
   
   ```
   cd /path/to/com.heimo.aurasync
   npm pack
   ```
   
   This will create a file named like `com.heimo.aurasync-1.0.0.tgz`

### 2. Git Repository Distribution

To distribute via Git:

1. Make sure your package structure is at the root of the repository
2. Create a tag for your version:

   ```
   git tag v1.0.0
   git push --tags
   ```

3. Users can add to their project's `Packages/manifest.json`:

   ```json
   "com.heimo.aurasync": "https://github.com/heimo/aurasync.git#v1.0.0"
   ```

### 3. Unity Asset Store Distribution

For Asset Store submission:

1. Follow Unity''s Asset Store publishing guidelines
2. Make sure your package complies with all Asset Store requirements
3. Create Asset Store metadata files as required
4. Submit through the Asset Store Publisher portal

## Updating the Package

When releasing a new version:

1. Update the `version` field in `package.json`
2. Update `CHANGELOG.md` with details about the changes
3. For Git distribution, create a new version tag:
   ```
   git tag v1.1.0
   git push --tags
   ```

## Best Practices

- Keep your versioning consistent (follow semantic versioning)
- Document all changes in the CHANGELOG.md file
- Test your package thoroughly before distribution
- Provide clear installation and usage instructions in your README.md
