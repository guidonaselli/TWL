<?xml version="1.0" encoding="UTF-8"?>
<tileset version="1.10" tiledversion="1.10.2" name="tiraka_tileset"
         tilewidth="32" tileheight="32" tilecount="256" columns="16">
  <image source="tiraka_tileset.png" width="512" height="512"/>
  <!-- Conventions (tile IDs are 0-based within the tileset):
       0..2  sand
       4..6  grass
       20..21 dirt path
       32..33 wet soil
       64..65 rock flats
       96 deep water, 97 shallow water, 112 foam overlay
  -->
  <tile id="64"><properties><property name="blocking" type="bool" value="true"/></properties></tile>
  <tile id="65"><properties><property name="blocking" type="bool" value="true"/></properties></tile>
  <tile id="96"><properties>
      <property name="walkable" type="bool" value="false"/>
      <property name="water" type="bool" value="true"/>
  </properties></tile>
  <tile id="97"><properties>
      <property name="walkable" type="bool" value="false"/>
      <property name="water" type="bool" value="true"/>
  </properties></tile>
</tileset>
