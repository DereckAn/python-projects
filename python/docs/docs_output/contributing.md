---
url: https://docs.fabricmc.net/contributing
source: docs.fabricmc.net
scrape_date: 2025-06-19 05:09:35
---

# Contribution Guidelines | Fabric Documentation



# Contribution Guidelines

  


This page is written for version:

 **1.21.4**

  


This website uses [VitePress](<https://vitepress.dev/>) to generate static HTML from various Markdown files. You should familiarize yourself with the Markdown extensions that VitePress supports [here](<https://vitepress.dev/guide/markdown#features>).

There are three ways you can contribute to this website:

  * Translating Documentation
  * Contributing Content
  * Contributing Framework



All contributions must follow our style guidelines.

## Translating Documentation ​

If you want to translate the documentation into your language, you can do this on the [Fabric Crowdin page](<https://crowdin.com/project/fabricmc>).

## new-content Contributing Content ​

Content contributions are the main way to contribute to the Fabric Documentation.

All content contributions go through the following stages, each of which is associated with a label:

  1. locally Prepare your changes and push a PR
  2. stage:expansion: Guidance for Expansion if needed
  3. stage:verification: Content verification
  4. stage:cleanup: Grammar, Linting...
  5. stage:ready: Ready to be merged!



All content must follow our style guidelines.

### 1\. Prepare Your Changes ​

This website is open-source, and it is developed in a GitHub repository, which means that we rely on the GitHub flow:

  1. [Fork the GitHub repository](<https://github.com/FabricMC/fabric-docs/fork>)
  2. Create a new branch on your fork
  3. Make your changes on that branch
  4. Open a Pull Request to the original repository



You can read more about the GitHub flow [here](<https://docs.github.com/en/get-started/using-github/github-flow>).

You can either make changes from the web UI on GitHub, or you can develop and preview the website locally.

#### Cloning Your Fork ​

If you want to develop locally, you will need to install [Git](<https://git-scm.com/>).

After that, clone your fork of the repository with:

sh
    
    
    # make sure to replace "your-username" with your actual username
    git clone https://github.com/your-username/fabric-docs.git

1  
2  


#### Installing Dependencies ​

If you want to preview your changes locally, you will need to install [Node.js 18+](<https://nodejs.org/en/>).

After that, make sure to install all dependencies with:

sh
    
    
    npm install

1  


#### Running the Development Server ​

This will allow you to preview your changes locally at `localhost:5173` and will automatically reload the page when you make changes.

sh
    
    
    npm run dev

1  


Now you can open and browse the website from the browser by visiting `http://localhost:5173`.

#### Building the Website ​

This will compile all Markdown files into static HTML files and place them in `.vitepress/dist`:

sh
    
    
    npm run build

1  


#### Previewing the Built Website ​

This will start a local server on port `4173` serving the content found in `.vitepress/dist`:

sh
    
    
    npm run preview

1  


#### Opening a Pull Request ​

Once you're happy with your changes, you may `push` your changes:

sh
    
    
    git add .
    git commit -m "Description of your changes"
    git push

1  
2  
3  


Then, follow the link in the output of `git push` to open a PR.

### 2\. stage:expansion Guidance for Expansion if Needed ​

If the documentation team thinks that you could expand upon your pull request, a member of the team will add the stage:expansion label to your pull request alongside a comment explaining what they think you could expand upon. If you agree with the suggestion, you can expand upon your pull request.

If you do not want to expand upon your pull request, but you are happy for someone else to expand upon it at a later date, you should create an issue on the [Issues page](<https://github.com/FabricMC/fabric-docs/issues>) and explain what you think could be expanded upon. The documentation team will then add the help-wanted label to your PR.

### 3\. stage:verification Content Verification ​

This is the most important stage as it ensures that the content is accurate and follows the Fabric Documentation style guide.

In this stage, the following questions should be answered:

  * Is all of the content correct?
  * Is all of the content up-to-date?
  * Does the content cover all cases, such as different operating systems?



### 4\. stage:cleanup Cleanup ​

In this stage, the following happens:

  * Fixing of any grammar issues using [LanguageTool](<https://languagetool.org/>)
  * Linting of all Markdown files using [`markdownlint`](<https://github.com/DavidAnson/markdownlint>)
  * Formatting of all Java code using [Checkstyle](<https://checkstyle.sourceforge.io/>)
  * Other miscellaneous fixes or improvements



## framework Contributing Framework ​

Framework refers to the internal structure of the website, any pull requests that modify the framework of the website will be labeled with the framework label.

You should really only make framework pull requests after consulting with the documentation team on the [Fabric Discord](<https://discord.gg/v6v4pMv>) or via an issue.

INFO

Modifying sidebar files and the navigation bar configuration does not count as a framework pull request.

## Style Guidelines ​

If you are unsure about anything, you can ask in the [Fabric Discord](<https://discord.gg/v6v4pMv>) or via GitHub Discussions.

### Write the Original in American English ​

All original documentation is written in English, following the American rules of grammar.

### Add Data to the Frontmatter ​

Each page must have a `title` and a `description` in the frontmatter.

Remember to also add your GitHub username to `authors` in the frontmatter of the Markdown file! This way we can give you proper credit.

yaml
    
    
    ---
    title: Title of the Page
    description: This is the description of the page.
    authors:
      - your-username
    ---

1  
2  
3  
4  
5  
6  


### Add Anchors to Headings ​

Each heading must have an anchor, which is used to link to that heading:

md
    
    
    ## This Is a Heading {#this-is-a-heading}

1  


The anchor must use lowercase characters, numbers and dashes.

### Place Code Within the `/reference` Mod ​

If you create or modify pages containing code, place the code in an appropriate location within the reference mod (located in the `/reference` folder of the repository). Then, use the [code snippet feature offered by VitePress](<https://vitepress.dev/guide/markdown#import-code-snippets>) to embed the code.

For example, to highlight lines 15-21 of the `FabricDocsReference.java` file from the reference mod:

mdjava

md
    
    
    <<< @/reference/latest/src/main/java/com/example/docs/FabricDocsReference.java{15-21}

1  


java
    
    
    package com.example.docs;
    
    import org.slf4j.Logger;
    import org.slf4j.LoggerFactory;
    
    import net.minecraft.particle.SimpleParticleType;
    import net.minecraft.registry.Registries;
    import net.minecraft.registry.Registry;
    import net.minecraft.util.Identifier;
    
    import net.fabricmc.api.ModInitializer;
    import net.fabricmc.fabric.api.particle.v1.FabricParticleTypes;
    
    //#entrypoint
    public class FabricDocsReference implements ModInitializer {
    	// This logger is used to write text to the console and the log file.
    	// It is considered best practice to use your mod id as the logger's name.
    	// That way, it's clear which mod wrote info, warnings, and errors.
    	public static final String MOD_ID = "fabric-docs-reference";
    	public static final Logger LOGGER = LoggerFactory.getLogger(MOD_ID);
    
    	//#entrypoint
    	//#particle_register_main
    	// This DefaultParticleType gets called when you want to use your particle in code.
    	public static final SimpleParticleType SPARKLE_PARTICLE = FabricParticleTypes.simple();
    
    	//#particle_register_main
    	//#entrypoint
    	@Override
    	public void onInitialize() {
    		// This code runs as soon as Minecraft is in a mod-load-ready state.
    		// However, some things (like resources) may still be uninitialized.
    		// Proceed with mild caution.
    
    		LOGGER.info("Hello Fabric world!");
    		//#entrypoint
    
    		//#particle_register_main
    		// Register our custom particle type in the mod initializer.
    		Registry.register(Registries.PARTICLE_TYPE, Identifier.of(MOD_ID, "sparkle_particle"), SPARKLE_PARTICLE);
    		//#particle_register_main
    		//#entrypoint
    	}
    }

1  
2  
3  
4  
5  
6  
7  
8  
9  
10  
11  
12  
13  
14  
15  
16  
17  
18  
19  
20  
21  
22  
23  
24  
25  
26  
27  
28  
29  
30  
31  
32  
33  
34  
35  
36  
37  
38  
39  
40  
41  
42  
43  
44  


If you need a greater span of control, you can use the [transclude feature from `markdown-it-vuepress-code-snippet-enhanced`](<https://github.com/fabioaanthony/markdown-it-vuepress-code-snippet-enhanced>).

For example, this will embed the sections of the file above that are marked with the `#entrypoint` tag:

mdjava

md
    
    
    @[code transcludeWith=#entrypoint](@/reference/latest/src/main/java/com/example/docs/FabricDocsReference.java)

1  


java
    
    
    public class FabricDocsReference implements ModInitializer {
    	// This logger is used to write text to the console and the log file.
    	// It is considered best practice to use your mod id as the logger's name.
    	// That way, it's clear which mod wrote info, warnings, and errors.
    	public static final String MOD_ID = "fabric-docs-reference";
    	public static final Logger LOGGER = LoggerFactory.getLogger(MOD_ID);
    
    	@Override
    	public void onInitialize() {
    		// This code runs as soon as Minecraft is in a mod-load-ready state.
    		// However, some things (like resources) may still be uninitialized.
    		// Proceed with mild caution.
    
    		LOGGER.info("Hello Fabric world!");
    	}
    }

1  
2  
3  
4  
5  
6  
7  
8  
9  
10  
11  
12  
13  
14  
15  
16  


### Create a Sidebar for Each New Section ​

If you're creating a new section, you should create a new sidebar in the `.vitepress/sidebars` folder and add it to the `i18n.mts` file.

If you need assistance with this, please ask in the [Fabric Discord](<https://discord.gg/v6v4pMv>)'s `#docs` channel.

### Add New Pages to the Relevant Sidebars ​

When creating a new page, you should add it to the relevant sidebar in the `.vitepress/sidebars` folder.

Again, if you need assistance, ask in the Fabric Discord in the `#docs` channel.

### Place Media in `/assets` ​

Any images should be placed in a suitable place in the `/public/assets` folder.

### Use Relative Links! ​

This is because of the versioning system in place, which will process the links to add the version beforehand. If you use absolute links, the version number will not be added to the link.

You must also not add the file extension to the link either.

For example, to link to the page found in `/players/index.md` from the page `/develop/index.md`, you would have to do the following:

✅ Correct❌ Wrong❌ Wrong

md
    
    
    This is a relative link!
    [Page](../players/index)

md
    
    
    This is an absolute link.
    [Page](/players/index)

md
    
    
    This relative link has the file extension.
    [Page](../players/index.md)
