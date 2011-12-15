@echo off
erase *.msi 
erase *.wixpdb
autopackage template.autopkg outercurve.autopkg coapp.toolkit.autopkg 
