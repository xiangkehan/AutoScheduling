# Product Overview

AutoScheduling3 is an intelligent guard duty scheduling system built with WinUI 3 for Windows. It's designed specifically for managing personnel assignments to guard positions (哨位) using constraint satisfaction and greedy algorithms.

## Core Purpose

Automate complex guard duty scheduling while respecting hard constraints (must-satisfy rules) and optimizing soft constraints (preference-based goals).

## Key Features

- **Smart Scheduling Algorithm**: Greedy algorithm with feasibility tensor supporting multiple hard and soft constraints
- **Complete Data Management**: Full lifecycle management for personnel, positions, skills, and constraints
- **Draft & Template System**: Save scheduling drafts and reuse templates
- **History & Comparison**: Complete history tracking with version comparison
- **Import/Export**: Bulk data import/export for migration and backup
- **Modern UI**: Fluent WinUI 3 interface with theme switching and animations

## Domain Concepts

- **Personnel (人员)**: Guards who can be assigned to positions
- **Position (哨位)**: Guard posts that need coverage
- **Skill (技能)**: Capabilities required for positions and possessed by personnel
- **Time Slot (时段)**: 12 two-hour periods per day (0-11)
- **Night Shift (夜哨)**: Slots 11, 0, 1, 2 (22:00-06:00)
- **Day Shift (日哨)**: Slots 3-10 (06:00-22:00)
- **Hard Constraints**: Must be satisfied (e.g., skill matching, availability)
- **Soft Constraints**: Optimization goals (e.g., rest balance, fair distribution)
