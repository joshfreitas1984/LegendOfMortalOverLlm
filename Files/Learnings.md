# Learnings

## Prompt Engineering

You need a lot of test scenarios so you know your new prompt has not messed with your normal scenarios.
Big prompts take a lot of the context window - you are better off dynamically adding prompts based on the input. 
Example: Do not add whole glossary, add entries for glossary when you see it in the raw
Example: Rules for placeholders or html if you see it in the raw
Use a Prompt Optimisation test to refine the prompt using the model.

## Correction Prompts
The model doesnt like it when we put corrections on new lines. It seems to work better when you are direct in a singular sentence.

With big prompts, its possible for system prompt to be lost out of the context - unless you expand the context window or minimise prompts.

## Glossary
Great for keeping consistant translation and fixing hallucinations.
Do not add too many glossary entries as it causes a lot of hallucinations.
Early findings on Auto Extraction - models do not find tags/glossary entries well enough. Need better prompts or classifications.