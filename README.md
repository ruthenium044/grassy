# Grassy
Just a grass shader

## Work progress

I started by doing lots of research on what ways I should generate grass and what are the pros and cons. Then I worked on generating the grass blades, shaping them, randomizing, and distributing them over the mesh. The properties were tightly dependent on each other, so it took some time to fine-tune them.
At the same time, I was making the shader for the blades. The goal was to make a not-too-complex URP shader with lighting and shadow casting. Sadly the proper lighting was not possible due to the blades being one-sided and not producing opposite normals for backside lights. But shadowcasting and gradient compensated for it.
I made the grass blades randomized with adjustment settings, different colour settings and other customizations to achieve the desired look for any case. 
Lastly, the wind and interaction system took a lot of effort. It exposed some other bugs on the way. I implemented a couple of different wind systems to test out how the grass would behave and what would look the best in my case. On top of everything I had to make the whole system customizable because it relies on an input mesh and certain things must be adjusted by hand and don’t scale well with different vertex inputs and distributions.
I relied on a lot of resources on the topics. There were a lot of different techniques and ways of doing it so I had to pick up what I needed for my specific case. 
Main features I worked on:

Lights, shadows, transparency 

![image](https://github.com/user-attachments/assets/3ad767a6-5a23-4b1e-9efc-5e59df744796)

Textures and different colourings

![image](https://github.com/user-attachments/assets/afb684ff-a5aa-4aae-b8f6-0339950ca59c)

Height tint colour

![image](https://github.com/user-attachments/assets/e7b28a96-6483-42c8-84e3-708cdae5fc61)

LOD system 

![image](https://github.com/user-attachments/assets/96d6c116-9a1f-4afb-9ccf-12a4b4dbeeea)

Grass blade displacement

<img alt="image" src="https://github.com/user-attachments/assets/9b0e3639-0b87-4344-a043-a6222861241b" />

Grass appearance and settings to shape it

<img alt="image" src="https://github.com/user-attachments/assets/b830d2c1-11e2-45af-b7ca-a66732169e43" />

Wind (can’t see it well on screenshots)

<img alt="image" src="https://github.com/user-attachments/assets/886b8e8f-df7f-49a6-b856-e25bab8ef7e4" />

## Goals

I feel like I reached my goals and learned things I did not fully expect to encounter on such a small-scope project. Overall I did the base of the project which was to generate the grass itself and even managed to hit some additional improvements such as wind, interaction, LOD and some other optimizations. 

## Time estimate

My time estimate was not accurate, because I expected the project to go smoother. After all, references seemed straightforward. But the main goals fit in the timeline because I was under-scoped at the start of the project.

## New knowledge

I learned to use compute shaders and generate meshes and got an introduction to using command buffers. I also learned to write shaders for URP, using built-in functionality for quick solutions. 

 
## Challenges

The first thing I encountered was generating the grass blade and shaping it to look like stylized grass. I tried to overcomplicate it and make it very smooth but in the end, the less complex shape was sufficient and even better.

![image](https://github.com/user-attachments/assets/bd81a162-54f3-4ecb-872b-87cca02a7501)
![image](https://github.com/user-attachments/assets/7a2e000a-867a-4c79-bf72-9feddf0497be)
![image](https://github.com/user-attachments/assets/6b1b0f42-15b0-46f2-a17c-7cb003dfb811)
![image](https://github.com/user-attachments/assets/b14d0d64-e87d-4333-ab5c-0ea69eba0686)
![image](https://github.com/user-attachments/assets/ff3200bd-c990-42cf-b442-3f2098a51905)
![image](https://github.com/user-attachments/assets/ae10bf1e-68df-477f-abdc-0c6d0576d02f)

Another big thing was distributing grass blades over a mesh without it being fully dependent on the vertices amount on the mesh. And fixing the glitched random position on the triangle introduced. My solution of generating random points on a triangle changed the whole compute shader structure and changed other parts of the functionality significantly. It introduced very specific bugs, but I solved them by adapting the system with the new limitations.
Additionally, later it came to my attention that the grass could be generated on a plane in two ways. One way would be using 2 threads for 2 dimensions and filling in the preset size plane area with the grass and the other one is using 1 thread to read the whole preset mesh and not be limited on the kinds of mesh the grass could be generated on. I chose the latter because I wanted to be able to generate the grass on any imputed mesh. It might have been not the best or most optimal option, but I wanted to have more versatile grass. 

![image](https://github.com/user-attachments/assets/0572f560-9c9b-48cd-ac53-5945b05bcb28)
![image](https://github.com/user-attachments/assets/b43cf97b-5c48-4240-854d-f553c69110c6)
![image](https://github.com/user-attachments/assets/8a72ebd1-4e7d-47de-b110-a859569ae12e)
![image](https://github.com/user-attachments/assets/210ddd53-7fc9-47c2-97c3-6378be4e3397)
![image](https://github.com/user-attachments/assets/4673837c-c6b4-4aa5-bd36-a9c8b44a35a6)

Passing generated data to the shader without losing it, mapping it correctly to the generated meshes. Also, making the shader for the grass blades with lighting and all other basic properties. I had to limit the lighting on the grass because if I wanted proper lighting I would need to have double-sided grass and that was outside the scope and not worth it because the effect was not noticeable enough anyway.

![image](https://github.com/user-attachments/assets/3be4a4eb-a3a1-491c-99c6-4bdf32dd7103)
![image](https://github.com/user-attachments/assets/e7651157-c745-4afe-83e2-280b54f274d1)

Wind and interaction also needed to be fine-tuned and took some experimentation to achieve the desired look. The biggest challenge was to make the grass stylized but still behave and look realistically enough, for example not looking like seaweed. Additionally, I added some colour variance settings to change the different lengths of grass tint colour. 

![image](https://github.com/user-attachments/assets/68a6bbc5-19aa-4e70-b7fb-826f371f1892)


